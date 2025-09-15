using CertVal.EmailService.Configuration;
using CertVal.EmailService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CertVal.EmailService.Services;

public class RabbitMqService : IRabbitMqService
{
    private readonly EmailServiceConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConnection _connection;

    private IModel? _channel;
    private string? _consumerTag;
    private bool _disposed;

    public RabbitMqService(
        IConnection connection,
        IOptions<EmailServiceConfiguration> configuration,
        IEmailService emailService,
        ILogger<RabbitMqService> logger)
    {
        _connection = connection;
        _configuration = configuration.Value;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SetupQueueAsync();
            await StartConsumerAsync(cancellationToken);

            _logger.LogInformation("RabbitMQ consumer started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer");
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag) && _channel != null)
            {
                _channel.BasicCancel(_consumerTag);
                _consumerTag = null;
            }

            _logger.LogInformation("RabbitMQ consumer stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping RabbitMQ consumer");
        }
    }

    private Task SetupQueueAsync()
    {
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _configuration.ExchangeName,
            type: ExchangeType.Direct,
            durable: true);

        _channel.QueueDeclare(
            queue: _configuration.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: _configuration.QueueName,
            exchange: _configuration.ExchangeName,
            routingKey: _configuration.RoutingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("Queue setup completed: {QueueName}", _configuration.QueueName);

        return Task.CompletedTask;
    }

    private Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        if (_channel == null) throw new InvalidOperationException("Channel not initialized");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                await ProcessMessageAsync(ea, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _consumerTag = _channel.BasicConsume(
            queue: _configuration.QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var body = eventArgs.Body.ToArray();
        var messageJson = Encoding.UTF8.GetString(body);

        try
        {
            _logger.LogDebug("Processing message: {MessageId}", eventArgs.BasicProperties.MessageId);

            var message = JsonSerializer.Deserialize<EmailNotificationMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {MessageJson}", messageJson);
                _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            if (message.RetryCount >= _configuration.MaxRetryAttempts)
            {
                _logger.LogError("Message {MessageId} exceeded max retry attempts ({MaxRetries}). Moving to dead letter queue.",
                    message.MessageId, _configuration.MaxRetryAttempts);

                _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var success = await _emailService.SendEmailAsync(message);

            if (success)
            {
                _channel!.BasicAck(eventArgs.DeliveryTag, multiple: false);
                _logger.LogInformation("Successfully processed email message {MessageId} of type {Type}",
                    message.MessageId, message.Type);
            }
            else
            {
                var retryMessage = message with { RetryCount = message.RetryCount + 1 };

                _logger.LogWarning("Failed to send email {MessageId}. Retry {RetryCount}/{MaxRetries}",
                    message.MessageId, retryMessage.RetryCount, _configuration.MaxRetryAttempts);

                if (retryMessage.RetryCount < _configuration.MaxRetryAttempts)
                {
                    await Task.Delay(_configuration.RetryDelay, cancellationToken);
                    await RequeueMessageAsync(retryMessage);
                }

                _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message: {MessageJson}", messageJson);
            _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private Task RequeueMessageAsync(EmailNotificationMessage message)
    {
        if (_channel == null) return Task.CompletedTask;

        try
        {
            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = _channel.CreateBasicProperties();
            properties.MessageId = message.MessageId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Persistent = true;
            properties.Type = message.Type.ToString();

            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                properties.CorrelationId = message.CorrelationId;
            }

            _channel.BasicPublish(
                exchange: _configuration.ExchangeName,
                routingKey: _configuration.RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Requeued message {MessageId} for retry {RetryCount}",
                message.MessageId, message.RetryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue message {MessageId}", message.MessageId);
        }

        return Task.CompletedTask;
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

        _disposed = true;
    }
}