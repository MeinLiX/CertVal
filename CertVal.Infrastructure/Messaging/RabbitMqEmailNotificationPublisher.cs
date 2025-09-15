using CertVal.Core.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CertVal.Infrastructure.Messaging;

public class RabbitMqEmailNotificationPublisher : IEmailNotificationPublisher
{
    private readonly IConnection _connection;
    private readonly MessagingConfiguration _messagingConfig;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEmailNotificationPublisher> _logger;
    private readonly Lazy<Task<IModel>> _channelLazy;
    private bool _disposed;

    public RabbitMqEmailNotificationPublisher(
        IConnection connection,
        IOptions<MessagingConfiguration> messagingOptions,
        IConfiguration configuration,
        ILogger<RabbitMqEmailNotificationPublisher> logger)
    {
        _connection = connection;
        _messagingConfig = messagingOptions.Value;
        _configuration = configuration;
        _logger = logger;

        _channelLazy = new Lazy<Task<IModel>>(CreateChannelAsync);
    }

    public async Task PublishAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await _channelLazy.Value;

            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = channel.CreateBasicProperties();
            properties.MessageId = message.MessageId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Persistent = _messagingConfig.PersistentMessages;
            properties.Type = message.Type.ToString();

            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                properties.CorrelationId = message.CorrelationId;
            }

            channel.BasicPublish(
                exchange: _messagingConfig.ExchangeName,
                routingKey: _messagingConfig.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published email notification: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish email notification: {MessageId}, Type: {Type}",
                message.MessageId, message.Type);
            throw;
        }
    }

    public async Task PublishUserRegisteredAsync(Guid userId, string email, string firstName, string lastName,
        string confirmationToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["EmailService:Templates:BaseUrl"] ?? "https://certval.halerka.dev";

        var message = new EmailNotificationMessage
        {
            Type = EmailNotificationType.UserRegistered,
            ToEmail = email,
            ToName = $"{firstName} {lastName}".Trim(),
            Data = new Dictionary<string, object>
            {
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["Email"] = email,
                ["ConfirmationToken"] = confirmationToken,
                ["BaseUrl"] = baseUrl
            },
            CorrelationId = userId.ToString()
        };

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishWorkspaceInvitationAsync(Guid workspaceId, string inviteeName, string inviterName,
        string workspaceName, string invitationToken, string role, string email,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["EmailService:Templates:BaseUrl"] ?? "https://certval.halerka.dev";

        var message = new EmailNotificationMessage
        {
            Type = EmailNotificationType.WorkspaceInvitation,
            ToEmail = email,
            ToName = inviteeName,
            Data = new Dictionary<string, object>
            {
                ["InviteeName"] = inviteeName,
                ["InviterName"] = inviterName,
                ["WorkspaceName"] = workspaceName,
                ["WorkspaceId"] = workspaceId.ToString(),
                ["InvitationToken"] = invitationToken,
                ["BaseUrl"] = baseUrl,
                ["Role"] = role
            },
            CorrelationId = workspaceId.ToString()
        };

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishPasswordResetAsync(string email, string firstName, string resetToken, DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["EmailService:Templates:BaseUrl"] ?? "https://certval.halerka.dev";

        var message = new EmailNotificationMessage
        {
            Type = EmailNotificationType.PasswordReset,
            ToEmail = email,
            ToName = firstName,
            Data = new Dictionary<string, object>
            {
                ["FirstName"] = firstName,
                ["ResetToken"] = resetToken,
                ["BaseUrl"] = baseUrl,
                ["ExpiresAt"] = expiresAt
            }
        };

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishCertificateExpiringAsync(string email, string workspaceName, string certificateSubject,
        string certificateIssuer, DateTime expiryDate, int daysUntilExpiry,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["EmailService:Templates:BaseUrl"] ?? "https://certval.halerka.dev";
        var notificationType = daysUntilExpiry <= 0 ? EmailNotificationType.CertificateExpired : EmailNotificationType.CertificateExpiring;

        var message = new EmailNotificationMessage
        {
            Type = notificationType,
            ToEmail = email,
            ToName = string.Empty,
            Data = new Dictionary<string, object>
            {
                ["WorkspaceName"] = workspaceName,
                ["CertificateSubject"] = certificateSubject,
                ["CertificateIssuer"] = certificateIssuer,
                ["ExpiryDate"] = expiryDate,
                ["DaysUntilExpiry"] = daysUntilExpiry,
                ["BaseUrl"] = baseUrl
            }
        };

        await PublishAsync(message, cancellationToken);
    }

    private async Task<IModel> CreateChannelAsync()
    {
        try
        {
            var channel = _connection.CreateModel();

            // Declare exchange
            channel.ExchangeDeclare(
                exchange: _messagingConfig.ExchangeName,
                type: ExchangeType.Direct,
                durable: _messagingConfig.DurableQueues);

            _logger.LogDebug("RabbitMQ channel created and exchange declared: {ExchangeName}", _messagingConfig.ExchangeName);

            return await Task.FromResult(channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        try
        {
            if (_channelLazy.IsValueCreated)
            {
                var channelTask = _channelLazy.Value;
                if (channelTask.IsCompletedSuccessfully)
                {
                    channelTask.Result?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ resources");
        }

        _disposed = true;
        return ValueTask.CompletedTask;
    }
}