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
    private readonly object _channelLock = new();

    private IModel? _channel;
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

        _connection.CallbackException += (_, args) =>
            _logger.LogError(args.Exception, "RabbitMQ connection callback exception");
        _connection.ConnectionShutdown += (_, reason) =>
            _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText}", reason.ReplyText);
        _connection.ConnectionBlocked += (_, reason) =>
            _logger.LogWarning("RabbitMQ connection blocked: {Reason}", reason.Reason);
        _connection.ConnectionUnblocked += (_, __) =>
            _logger.LogInformation("RabbitMQ connection unblocked");
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RabbitMqService));

        try
        {
            _logger.LogInformation("Config: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
                _config.ExchangeName, _config.EmailQueueName, _config.EmailRoutingKey);

            SetupQueue();
            StartConsumer();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => _ = StopConsumingAsync());
            }

            LogQueueStatus();
            _logger.LogInformation("Started consuming messages from queue: {QueueName}", _config.EmailQueueName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
            throw;
        }
    }

    public Task StopConsumingAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag) && _channel?.IsOpen == true)
            {
                _channel.BasicCancel(_consumerTag);
                _consumerTag = null;
                _logger.LogInformation("Stopped consuming messages");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumer");
        }

        return Task.CompletedTask;
    }

    private void SetupQueue()
    {
        EnsureChannel();
    }

    private void StartConsumer()
    {
        var channel = EnsureChannel();
        if (!string.IsNullOrEmpty(_consumerTag)) return;

        var consumer = new EventingBasicConsumer(channel);

        consumer.Registered += (_, e) =>
        {
            _logger.LogInformation("Consumer registered: {ConsumerTag}", e.ConsumerTags.FirstOrDefault());
        };
        consumer.Shutdown += (_, e) =>
        {
            _logger.LogWarning("Consumer shutdown: {ReplyText}", e.ReplyText);
        };
        consumer.Received += async (_, eventArgs) =>
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

        _consumerTag = channel.BasicConsume(
            queue: _config.EmailQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("BasicConsume started. ConsumerTag={ConsumerTag}", _consumerTag);
    }

    private IModel EnsureChannel()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMqService));

        lock (_channelLock)
        {
            if (_channel?.IsOpen == true) return _channel;

            try
            {
                _channel?.Dispose();
                var ch = _connection.CreateModel();
                ch.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                ch.CallbackException += (_, args) =>
                    _logger.LogError(args.Exception, "RabbitMQ channel callback exception");
                ch.ModelShutdown += (_, reason) =>
                {
                    _logger.LogWarning("RabbitMQ channel shutdown: {ReplyText}", reason.ReplyText);
                    if (!reason.Initiator.Equals(ShutdownInitiator.Application))
                    {
                        _logger.LogInformation("Attempting to recover channel...");
                    }
                };

                DeclareTopology(ch);
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
    }

    private void DeclareTopology(IModel ch)
    {
        ch.ExchangeDeclare(
            exchange: _config.ExchangeName,
            type: ExchangeType.Direct,
            durable: _config.DurableQueues);

        ch.QueueDeclare(
            queue: _config.EmailQueueName,
            durable: _config.DurableQueues,
            exclusive: false,
            autoDelete: false);

        ch.QueueBind(
            queue: _config.EmailQueueName,
            exchange: _config.ExchangeName,
            routingKey: _config.EmailRoutingKey);

        var dlxName = $"{_config.ExchangeName}-dlx";
        var retryQueueName = $"{_config.EmailQueueName}-retry";

        ch.ExchangeDeclare(
            exchange: dlxName,
            type: ExchangeType.Direct,
            durable: _config.DurableQueues);

        var retryQueueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _config.ExchangeName,
            ["x-dead-letter-routing-key"] = _config.EmailRoutingKey,
            ["x-message-ttl"] = (int)_config.RetryDelay.TotalMilliseconds
        };

        ch.QueueDeclare(
            queue: retryQueueName,
            durable: _config.DurableQueues,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArgs);

        ch.QueueBind(
            queue: retryQueueName,
            exchange: dlxName,
            routingKey: _config.EmailRoutingKey);

        _logger.LogDebug("Declared topology: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}, RetryQueue={RetryQueue}",
            _config.ExchangeName, _config.EmailQueueName, _config.EmailRoutingKey, retryQueueName);
    }

    private void LogQueueStatus()
    {
        try
        {
            var channel = EnsureChannel();
            var result = channel.QueueDeclarePassive(_config.EmailQueueName);
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
                AcknowledgeMessage(eventArgs, success: false);
                return;
            }

            if (_idempotencyTracker.IsProcessed(message.MessageId))
            {
                _logger.LogWarning("Duplicate message {MessageId} detected, skipping", message.MessageId);
                _metrics.RecordMessageFailed(message.Type.ToString(), "duplicate_message");
                AcknowledgeMessage(eventArgs, success: true);
                return;
            }

            if (message.RetryCount >= _config.MaxRetryAttempts)
            {
                _logger.LogError("Message {MessageId} exceeded max retry attempts, discarding", message.MessageId);
                _metrics.RecordMessageFailed(message.Type.ToString(), "max_retries_exceeded");
                AcknowledgeMessage(eventArgs, success: false);
                return;
            }

            var success = await _emailService.SendEmailAsync(message);

            stopwatch.Stop();
            _metrics.RecordProcessingDuration(stopwatch.Elapsed.TotalMilliseconds, message.Type.ToString());
            _metrics.RecordMessageProcessed(message.Type.ToString(), success);

            if (success)
            {
                _idempotencyTracker.TryMarkAsProcessed(message.MessageId);
                AcknowledgeMessage(eventArgs, success: true);
                return;
            }

            await HandleFailedMessage(message, eventArgs);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid message format, rejecting");
            _metrics.RecordMessageFailed("unknown", "invalid_json");
            AcknowledgeMessage(eventArgs, success: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            if (message != null)
            {
                _metrics.RecordMessageFailed(message.Type.ToString(), "unexpected_error");
            }
            AcknowledgeMessage(eventArgs, success: false, requeue: true);
        }
        finally
        {
            LogQueueStatus();
        }
    }

    private async Task HandleFailedMessage(EmailNotificationMessage message, BasicDeliverEventArgs eventArgs)
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

        AcknowledgeMessage(eventArgs, success: false);
    }

    private void AcknowledgeMessage(BasicDeliverEventArgs eventArgs, bool success, bool requeue = false)
    {
        try
        {
            lock (_channelLock)
            {
                if (_channel?.IsOpen != true)
                {
                    _logger.LogWarning("Cannot acknowledge message: channel is not open");
                    return;
                }

                if (success)
                {
                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: requeue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message");
        }
    }

    private async Task RequeueMessageAsync(EmailNotificationMessage message)
    {
        var channel = EnsureChannel();

        try
        {
            var dlxName = $"{_config.ExchangeName}-dlx";
            var messageJson = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(messageJson);
            var properties = BuildProperties(message, channel);

            channel.BasicPublish(
                exchange: dlxName,
                routingKey: _config.EmailRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message {MessageId} queued for retry (attempt {RetryCount}/{MaxRetries})",
                message.MessageId, message.RetryCount + 1, _config.MaxRetryAttempts);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue message {MessageId}", message.MessageId);
        }
    }

    private static IBasicProperties BuildProperties(EmailNotificationMessage message, IModel channel)
    {
        var properties = channel.CreateBasicProperties();
        properties.MessageId = message.MessageId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Persistent = true;
        properties.Type = message.Type.ToString();

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            properties.CorrelationId = message.CorrelationId;
        }

        return properties;
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