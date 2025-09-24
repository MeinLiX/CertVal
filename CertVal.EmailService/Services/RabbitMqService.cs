using CertVal.Core.Messaging;
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

    private static readonly JsonSerializerOptions DeserializerOptions = new()
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
        _config = messagingOptions.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RabbitMqService));

        try
        {
            SetupQueue();
            StartConsumer();
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
            if (!string.IsNullOrEmpty(_consumerTag) && _channel != null)
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
        _channel = _connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _channel.ExchangeDeclare(
            exchange: _config.ExchangeName,
            type: ExchangeType.Direct,
            durable: _config.DurableQueues);

        _channel.QueueDeclare(
            queue: _config.EmailQueueName,
            durable: _config.DurableQueues,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: _config.EmailQueueName,
            exchange: _config.ExchangeName,
            routingKey: _config.EmailRoutingKey);
    }

    private void StartConsumer()
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, eventArgs) => await ProcessMessageAsync(eventArgs);

        _consumerTag = _channel.BasicConsume(
            queue: _config.EmailQueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            var message = JsonSerializer.Deserialize<EmailNotificationMessage>(messageBody, DeserializerOptions);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message, rejecting");
                AcknowledgeMessage(eventArgs, false);
                return;
            }

            if (message.RetryCount >= _config.MaxRetryAttempts)
            {
                _logger.LogError("Message {MessageId} exceeded max retry attempts, discarding", message.MessageId);
                AcknowledgeMessage(eventArgs, false);
                return;
            }

            var success = await _emailService.SendEmailAsync(message);

            if (success)
            {
                AcknowledgeMessage(eventArgs, true);
            }
            else
            {
                await HandleFailedMessage(message, eventArgs);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid message format, rejecting");
            AcknowledgeMessage(eventArgs, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            AcknowledgeMessage(eventArgs, false, requeue: true);
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

        AcknowledgeMessage(eventArgs, false);
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
        if (_channel == null) return;

        try
        {
            await Task.Delay(_config.RetryDelay);

            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(messageJson);
            var properties = _channel.CreateBasicProperties();
            properties.MessageId = message.MessageId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Persistent = _config.PersistentMessages;
            properties.Type = message.Type.ToString();

            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                properties.CorrelationId = message.CorrelationId;
            }

            _channel.BasicPublish(
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