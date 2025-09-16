using CertVal.Core.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CertVal.Infrastructure.Messaging;

public class RabbitMqEmailNotificationPublisher : IEmailNotificationPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly MessagingConfiguration _config;
    private readonly IConfiguration _appConfig;
    private readonly ILogger<RabbitMqEmailNotificationPublisher> _logger;
    private readonly Lazy<IModel> _channelLazy;
    private bool _disposed;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RabbitMqEmailNotificationPublisher(
        IConnection connection,
        IOptions<MessagingConfiguration> messagingOptions,
        IConfiguration configuration,
        ILogger<RabbitMqEmailNotificationPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = messagingOptions.Value ?? throw new ArgumentNullException(nameof(messagingOptions));
        _appConfig = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channelLazy = new Lazy<IModel>(CreateChannel);
    }

    public async Task PublishAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RabbitMqEmailNotificationPublisher));

        try
        {
            var channel = _channelLazy.Value;
            var messageJson = JsonSerializer.Serialize(message, SerializerOptions);
            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = CreateBasicProperties(channel, message);

            channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: _config.EmailRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published email notification: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish email notification: {MessageId}, Type: {Type}",
                message.MessageId, message.Type);
            throw;
        }
    }

    public async Task PublishUserRegisteredAsync(Guid userId, string email, string firstName,
        string lastName, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var message = CreateEmailMessage(
            EmailNotificationType.UserRegistered,
            email,
            $"{firstName} {lastName}".Trim(),
            new Dictionary<string, object>
            {
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["Email"] = email,
                ["ConfirmationToken"] = confirmationToken,
                ["BaseUrl"] = GetBaseUrl()
            },
            userId.ToString());

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishWorkspaceInvitationAsync(Guid workspaceId, string inviteeName,
        string inviterName, string workspaceName, string invitationToken, string role,
        string email, CancellationToken cancellationToken = default)
    {
        var message = CreateEmailMessage(
            EmailNotificationType.WorkspaceInvitation,
            email,
            inviteeName,
            new Dictionary<string, object>
            {
                ["InviteeName"] = inviteeName,
                ["InviterName"] = inviterName,
                ["WorkspaceName"] = workspaceName,
                ["WorkspaceId"] = workspaceId.ToString(),
                ["InvitationToken"] = invitationToken,
                ["BaseUrl"] = GetBaseUrl(),
                ["Role"] = role
            },
            workspaceId.ToString());

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishPasswordResetAsync(string email, string firstName, string resetToken,
        DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var message = CreateEmailMessage(
            EmailNotificationType.PasswordReset,
            email,
            firstName,
            new Dictionary<string, object>
            {
                ["FirstName"] = firstName,
                ["ResetToken"] = resetToken,
                ["BaseUrl"] = GetBaseUrl(),
                ["ExpiresAt"] = expiresAt
            });

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishCertificateExpiringAsync(string email, string workspaceName,
        string certificateSubject, string certificateIssuer, DateTime expiryDate,
        int daysUntilExpiry, CancellationToken cancellationToken = default)
    {
        var notificationType = daysUntilExpiry <= 0
            ? EmailNotificationType.CertificateExpired
            : EmailNotificationType.CertificateExpiring;

        var message = CreateEmailMessage(
            notificationType,
            email,
            string.Empty,
            new Dictionary<string, object>
            {
                ["WorkspaceName"] = workspaceName,
                ["CertificateSubject"] = certificateSubject,
                ["CertificateIssuer"] = certificateIssuer,
                ["ExpiryDate"] = expiryDate,
                ["DaysUntilExpiry"] = daysUntilExpiry,
                ["BaseUrl"] = GetBaseUrl()
            });

        await PublishAsync(message, cancellationToken);
    }

    private IModel CreateChannel()
    {
        try
        {
            var channel = _connection.CreateModel();

            // Declare exchange
            channel.ExchangeDeclare(
                exchange: _config.ExchangeName,
                type: ExchangeType.Direct,
                durable: _config.DurableQueues);

            // Declare queue
            channel.QueueDeclare(
                queue: _config.EmailQueueName,
                durable: _config.DurableQueues,
                exclusive: false,
                autoDelete: false);

            // Bind queue
            channel.QueueBind(
                queue: _config.EmailQueueName,
                exchange: _config.ExchangeName,
                routingKey: _config.EmailRoutingKey);

            _logger.LogDebug("RabbitMQ channel created and configured: Exchange={Exchange}, Queue={Queue}",
                _config.ExchangeName, _config.EmailQueueName);

            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
            throw;
        }
    }

    private IBasicProperties CreateBasicProperties(IModel channel, EmailNotificationMessage message)
    {
        var properties = channel.CreateBasicProperties();
        properties.MessageId = message.MessageId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Persistent = _config.PersistentMessages;
        properties.Type = message.Type.ToString();
        properties.ContentType = "application/json";
        properties.ContentEncoding = "utf-8";

        if (!string.IsNullOrEmpty(message.CorrelationId))
        {
            properties.CorrelationId = message.CorrelationId;
        }

        return properties;
    }

    private EmailNotificationMessage CreateEmailMessage(
        EmailNotificationType type,
        string email,
        string name,
        Dictionary<string, object> data,
        string? correlationId = null)
    {
        return new EmailNotificationMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Type = type,
            ToEmail = email,
            ToName = name,
            Data = data,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            CorrelationId = correlationId
        };
    }

    private string GetBaseUrl() =>
        _appConfig["EmailService:Templates:BaseUrl"] ?? "https://certval.halerka.dev";

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_channelLazy.IsValueCreated)
            {
                _channelLazy.Value?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ channel");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}