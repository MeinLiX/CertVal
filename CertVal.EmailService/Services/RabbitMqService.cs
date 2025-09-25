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
        ILogger<RabbitMqService> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = messagingOptions?.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        // Use synchronous EventingBasicConsumer to avoid DispatchConsumersAsync requirement
        var consumer = new EventingBasicConsumer(channel);

        consumer.Registered += (_, e) =>
        {
            _logger.LogInformation("Consumer registered: {ConsumerTag}", e.ConsumerTags.FirstOrDefault());
        };
        consumer.Shutdown += (_, e) =>
        {
            _logger.LogWarning("Consumer shutdown: {ReplyText}", e.ReplyText);
        };
        consumer.Received += (_, eventArgs) =>
        {
            try
            {
                // Process synchronously to keep single in-flight message per prefetch
                ProcessMessageAsync(eventArgs).GetAwaiter().GetResult();
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

        if (_channel?.IsOpen == true) return _channel;

        try
        {
            _channel?.Dispose();
            var ch = _connection.CreateModel();
            ch.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            ch.CallbackException += (_, args) => _logger.LogError(args.Exception, "RabbitMQ channel callback exception");
            ch.ModelShutdown += (_, reason) => _logger.LogWarning("RabbitMQ channel shutdown: {ReplyText}", reason.ReplyText);
            DeclareTopology(ch);
            _channel = ch;
            return ch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or configure RabbitMQ channel");
            throw;
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

        _logger.LogDebug("Declared topology: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
            _config.ExchangeName, _config.EmailQueueName, _config.EmailRoutingKey);
    }

    private void LogQueueStatus()
    {
        try
        {
            var channel = EnsureChannel();
            var result = channel.QueueDeclarePassive(_config.EmailQueueName);
            _logger.LogInformation("Queue status: {Queue} -> messages={MessageCount}, consumers={ConsumerCount}",
                _config.EmailQueueName, result.MessageCount, result.ConsumerCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query queue status for {Queue}", _config.EmailQueueName);
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            var message = JsonSerializer.Deserialize<EmailNotificationMessage>(messageBody, JsonOptions);

            if (message is null)
            {
                _logger.LogWarning("Failed to deserialize message, rejecting");
                AcknowledgeMessage(eventArgs, success: false);
                return;
            }

            if (message.RetryCount >= _config.MaxRetryAttempts)
            {
                _logger.LogError("Message {MessageId} exceeded max retry attempts, discarding", message.MessageId);
                AcknowledgeMessage(eventArgs, success: false);
                return;
            }

            var success = await _emailService.SendEmailAsync(message);

            if (success)
            {
                AcknowledgeMessage(eventArgs, success: true);
                return;
            }

            await HandleFailedMessage(message, eventArgs);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid message format, rejecting");
            AcknowledgeMessage(eventArgs, success: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
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
        }
        else
        {
            _logger.LogError("Message {MessageId} failed permanently, discarding", message.MessageId);
        }

        AcknowledgeMessage(eventArgs, success: false);
    }

    private void AcknowledgeMessage(BasicDeliverEventArgs eventArgs, bool success, bool requeue = false)
    {
        try
        {
            if (success)
            {
                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: requeue);
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
            await Task.Delay(_config.RetryDelay);

            var messageJson = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(messageJson);
            var properties = BuildProperties(message, channel);

            channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: _config.EmailRoutingKey,
                basicProperties: properties,
                body: body);
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