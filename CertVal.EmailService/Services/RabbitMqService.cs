using CertVal.Core.Messaging;
using CertVal.EmailService.Services.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CertVal.EmailService.Services;

public class RabbitMqService : IRabbitMqService
{
    private readonly MessagingConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConnection _connection;
    private readonly EmailServiceMetrics _metrics;
    private readonly IdempotencyTracker _idempotencyTracker;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    private IChannel? _channel;
    private string? _consumerTag;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RabbitMqService(
        IConnection connection,
        IOptions<MessagingConfiguration> messagingOptions,
        IEmailService emailService,
        ILogger<RabbitMqService> logger,
        EmailServiceMetrics metrics,
        IdempotencyTracker idempotencyTracker)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _idempotencyTracker = idempotencyTracker ?? throw new ArgumentNullException(nameof(idempotencyTracker));

        _connection.CallbackExceptionAsync += (_, args) =>
        {
            _logger.LogError(args.Exception, "RabbitMQ connection callback exception");
            return Task.CompletedTask;
        };
        _connection.ConnectionShutdownAsync += (_, reason) =>
        {
            _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText}", reason.ReplyText);
            return Task.CompletedTask;
        };
        _connection.ConnectionBlockedAsync += (_, reason) =>
        {
            _logger.LogWarning("RabbitMQ connection blocked: {Reason}", reason.Reason);
            return Task.CompletedTask;
        };
        _connection.ConnectionUnblockedAsync += (_, __) =>
        {
            _logger.LogInformation("RabbitMQ connection unblocked");
            return Task.CompletedTask;
        };
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RabbitMqService));

        try
        {
            _logger.LogInformation("Config: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
                _config.ExchangeName, _config.EmailQueueName, _config.EmailRoutingKey);

            await SetupQueueAsync();
            await StartConsumerAsync();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => _ = StopConsumingAsync());
            }

            await LogQueueStatusAsync();
            _logger.LogInformation("Started consuming messages from queue: {QueueName}", _config.EmailQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag) && _channel?.IsOpen == true)
            {
                await _channel.BasicCancelAsync(_consumerTag);
                _consumerTag = null;
                _logger.LogInformation("Stopped consuming messages");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumer");
        }
    }

    private async Task SetupQueueAsync()
    {
        await EnsureChannelAsync();
    }

    private async Task StartConsumerAsync()
    {
        var channel = await EnsureChannelAsync();
        if (!string.IsNullOrEmpty(_consumerTag)) return;

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.RegisteredAsync += (_, e) =>
        {
            _logger.LogInformation("Consumer registered: {ConsumerTag}", e.ConsumerTags.FirstOrDefault());
            return Task.CompletedTask;
        };
        consumer.ShutdownAsync += (_, e) =>
        {
            _logger.LogWarning("Consumer shutdown: {ReplyText}", e.ReplyText);
            return Task.CompletedTask;
        };
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await ProcessMessageAsync(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consumer Received handler");
            }
        };

        _consumerTag = await channel.BasicConsumeAsync(
            queue: _config.EmailQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("BasicConsume started. ConsumerTag={ConsumerTag}", _consumerTag);
    }

    private async Task<IChannel> EnsureChannelAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMqService));

        await _channelLock.WaitAsync();
        try
        {
            if (_channel?.IsOpen == true) return _channel;

            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    await _channel.DisposeAsync();
                }

                var ch = await _connection.CreateChannelAsync();
                await ch.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                ch.CallbackExceptionAsync += (_, args) =>
                {
                    _logger.LogError(args.Exception, "RabbitMQ channel callback exception");
                    return Task.CompletedTask;
                };
                ch.ChannelShutdownAsync += (_, reason) =>
                {
                    _logger.LogWarning("RabbitMQ channel shutdown: {ReplyText}", reason.ReplyText);
                    if (!reason.Initiator.Equals(ShutdownInitiator.Application))
                    {
                        _logger.LogInformation("Attempting to recover channel...");
                    }
                    return Task.CompletedTask;
                };

                await DeclareTopologyAsync(ch);
                _channel = ch;
                _logger.LogInformation("RabbitMQ channel created successfully");
                return ch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or configure RabbitMQ channel");
                throw;
            }
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task DeclareTopologyAsync(IChannel ch)
    {
        await ch.ExchangeDeclareAsync(
            exchange: _config.ExchangeName,
            type: ExchangeType.Direct,
            durable: _config.DurableQueues);

        await ch.QueueDeclareAsync(
            queue: _config.EmailQueueName,
            durable: _config.DurableQueues,
            exclusive: false,
            autoDelete: false);

        await ch.QueueBindAsync(
            queue: _config.EmailQueueName,
            exchange: _config.ExchangeName,
            routingKey: _config.EmailRoutingKey);

        var dlxName = $"{_config.ExchangeName}-dlx";
        var retryQueueName = $"{_config.EmailQueueName}-retry";

        await ch.ExchangeDeclareAsync(
            exchange: dlxName,
            type: ExchangeType.Direct,
            durable: _config.DurableQueues);

        var retryQueueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _config.ExchangeName,
            ["x-dead-letter-routing-key"] = _config.EmailRoutingKey,
            ["x-message-ttl"] = (int)_config.RetryDelay.TotalMilliseconds
        };

        await ch.QueueDeclareAsync(
            queue: retryQueueName,
            durable: _config.DurableQueues,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArgs);

        await ch.QueueBindAsync(
            queue: retryQueueName,
            exchange: dlxName,
            routingKey: _config.EmailRoutingKey);

        _logger.LogDebug("Declared topology: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}, RetryQueue={RetryQueue}",
            _config.ExchangeName, _config.EmailQueueName, _config.EmailRoutingKey, retryQueueName);
    }

    private async Task LogQueueStatusAsync()
    {
        try
        {
            var channel = await EnsureChannelAsync();
            var result = await channel.QueueDeclarePassiveAsync(_config.EmailQueueName);
            _logger.LogInformation("Queue status: {Queue} -> messages={MessageCount}, consumers={ConsumerCount}",
                _config.EmailQueueName, result.MessageCount, result.ConsumerCount);

            _metrics.UpdateQueueDepth((int)result.MessageCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query queue status for {Queue}", _config.EmailQueueName);
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.Span);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        EmailNotificationMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<EmailNotificationMessage>(messageBody, JsonOptions);

            if (message is null)
            {
                _logger.LogWarning("Failed to deserialize message, rejecting");
                _metrics.RecordMessageFailed("unknown", "deserialization_failed");
                await AcknowledgeMessageAsync(eventArgs, success: false);
                return;
            }

            if (_idempotencyTracker.IsProcessed(message.MessageId))
            {
                _logger.LogWarning("Duplicate message {MessageId} detected, skipping", message.MessageId);
                _metrics.RecordMessageFailed(message.Type.ToString(), "duplicate_message");
                await AcknowledgeMessageAsync(eventArgs, success: true);
                return;
            }

            if (message.RetryCount >= _config.MaxRetryAttempts)
            {
                _logger.LogError("Message {MessageId} exceeded max retry attempts, discarding", message.MessageId);
                _metrics.RecordMessageFailed(message.Type.ToString(), "max_retries_exceeded");
                await AcknowledgeMessageAsync(eventArgs, success: false);
                return;
            }

            var success = await _emailService.SendEmailAsync(message);

            stopwatch.Stop();
            _metrics.RecordProcessingDuration(stopwatch.Elapsed.TotalMilliseconds, message.Type.ToString());
            _metrics.RecordMessageProcessed(message.Type.ToString(), success);

            if (success)
            {
                _idempotencyTracker.TryMarkAsProcessed(message.MessageId);
                await AcknowledgeMessageAsync(eventArgs, success: true);
                return;
            }

            await HandleFailedMessageAsync(message, eventArgs);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid message format, rejecting");
            _metrics.RecordMessageFailed("unknown", "invalid_json");
            await AcknowledgeMessageAsync(eventArgs, success: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            if (message != null)
            {
                _metrics.RecordMessageFailed(message.Type.ToString(), "unexpected_error");
            }
            await AcknowledgeMessageAsync(eventArgs, success: false, requeue: true);
        }
        finally
        {
            await LogQueueStatusAsync();
        }
    }

    private async Task HandleFailedMessageAsync(EmailNotificationMessage message, BasicDeliverEventArgs eventArgs)
    {
        if (message.RetryCount + 1 < _config.MaxRetryAttempts)
        {
            var retryMessage = message with { RetryCount = message.RetryCount + 1 };
            await RequeueMessageAsync(retryMessage);
            _metrics.RecordMessageRetried(message.Type.ToString(), retryMessage.RetryCount);
        }
        else
        {
            _logger.LogError("Message {MessageId} failed permanently, discarding", message.MessageId);
            _metrics.RecordMessageFailed(message.Type.ToString(), "permanent_failure");
        }

        await AcknowledgeMessageAsync(eventArgs, success: false);
    }

    private async Task AcknowledgeMessageAsync(BasicDeliverEventArgs eventArgs, bool success, bool requeue = false)
    {
        try
        {
            var channel = await EnsureChannelAsync();
            if (channel.IsOpen != true)
            {
                _logger.LogWarning("Cannot acknowledge message: channel is not open");
                return;
            }

            if (success)
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: requeue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message");
        }
    }

    private async Task RequeueMessageAsync(EmailNotificationMessage message)
    {
        var channel = await EnsureChannelAsync();

        try
        {
            var dlxName = $"{_config.ExchangeName}-dlx";
            var messageJson = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = new BasicProperties
            {
                MessageId = message.MessageId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Persistent = true,
                Type = message.Type.ToString()
            };

            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                properties.CorrelationId = message.CorrelationId;
            }

            await channel.BasicPublishAsync(
                exchange: dlxName,
                routingKey: _config.EmailRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message {MessageId} queued for retry (attempt {RetryCount}/{MaxRetries})",
                message.MessageId, message.RetryCount + 1, _config.MaxRetryAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue message {MessageId}", message.MessageId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            await StopConsumingAsync();
            _channel?.Dispose();

            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ resources");
        }
        finally
        {
            _disposed = true;
        }
    }
}