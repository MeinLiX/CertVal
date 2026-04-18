using CertVal.Core.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CertVal.Infrastructure.Messaging;

public class RabbitMqEmailNotificationPublisher : IEmailNotificationPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly MessagingConfiguration _config;
    private readonly IConfiguration _appConfig;
    private readonly ILogger<RabbitMqEmailNotificationPublisher> _logger;
    private IChannel? _channel;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
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
    }

    public async Task PublishAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RabbitMqEmailNotificationPublisher));

        try
        {
            var channel = await GetChannelAsync();
            var messageJson = JsonSerializer.Serialize(message, SerializerOptions);
            var body = Encoding.UTF8.GetBytes(messageJson);

            var properties = new BasicProperties
            {
                MessageId = message.MessageId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Persistent = _config.PersistentMessages,
                Type = message.Type.ToString(),
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                properties.CorrelationId = message.CorrelationId;
            }

            await channel.BasicPublishAsync(
                exchange: _config.ExchangeName,
                routingKey: _config.EmailRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Published email notification: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);
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
                ["DaysUntilExpiry"] = daysUntilExpiry
            });

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishCertificateExpiringAggregatedAsync(IEnumerable<string> emails, string workspaceName,
        string certificateSubject, string certificateIssuer, DateTime expiryDate, int daysUntilExpiry,
        CancellationToken cancellationToken = default)
    {
        var emailList = emails.Distinct().ToList();
        if (emailList.Count == 0) return;

        var notificationType = daysUntilExpiry <= 0
            ? EmailNotificationType.CertificateExpired
            : EmailNotificationType.CertificateExpiring;

        var primary = emailList[0];
        var message = new EmailNotificationMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Type = notificationType,
            ToEmail = primary,
            ToName = string.Empty,
            Recipients = emailList,
            Data = new Dictionary<string, object>
            {
                ["WorkspaceName"] = workspaceName,
                ["CertificateSubject"] = certificateSubject,
                ["CertificateIssuer"] = certificateIssuer,
                ["ExpiryDate"] = expiryDate,
                ["DaysUntilExpiry"] = daysUntilExpiry
            },
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await PublishAsync(message, cancellationToken);
    }

    public async Task PublishCertificateExpiryDigestAsync(CertificateExpiryDigestMessage digest, CancellationToken cancellationToken = default)
    {
        var recipients = digest.Recipients.Distinct().ToList();
        if (recipients.Count == 0 || digest.Items.Count == 0) return;

        var items = digest.Items.Select(i => new Dictionary<string, object>
        {
            ["Subject"] = i.Subject,
            ["Issuer"] = i.Issuer,
            ["SerialNumber"] = i.SerialNumber ?? string.Empty,
            ["ExpiryDate"] = i.ExpiryDate,
            ["DaysUntilExpiry"] = i.DaysUntilExpiry,
            ["StatusLabel"] = i.IsExpired ? "Expired" : $"Expires in {i.DaysUntilExpiry} day(s)",
            ["StatusClass"] = i.IsExpired ? "status-expired" : "status-soon"
        }).Cast<object>().ToList();

        var summaryParts = new List<string>();
        if (digest.ExpiredCount > 0) summaryParts.Add($"{digest.ExpiredCount} expired");
        if (digest.ExpiringCount > 0) summaryParts.Add($"{digest.ExpiringCount} expiring soon");
        var summaryLine = string.Join(" · ", summaryParts);

        var remainingLine = digest.RemainingCount > 0
            ? $"…and {digest.RemainingCount} more certificate(s) not shown here."
            : string.Empty;

        var message = new EmailNotificationMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Type = EmailNotificationType.CertificateExpiryDigest,
            ToEmail = recipients[0],
            ToName = string.Empty,
            Recipients = recipients,
            CorrelationId = digest.CorrelationId,
            Data = new Dictionary<string, object>
            {
                ["WorkspaceName"] = digest.WorkspaceName,
                ["Items"] = items,
                ["TotalCount"] = digest.TotalCount,
                ["ShownCount"] = digest.Items.Count,
                ["RemainingCount"] = digest.RemainingCount,
                ["ExpiredCount"] = digest.ExpiredCount,
                ["ExpiringCount"] = digest.ExpiringCount,
                ["SummaryLine"] = summaryLine,
                ["RemainingLine"] = remainingLine
            },
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await PublishAsync(message, cancellationToken);
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel != null) return _channel;

        await _channelLock.WaitAsync();
        try
        {
            if (_channel != null) return _channel;
            _channel = await CreateChannelAsync();
            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task<IChannel> CreateChannelAsync()
    {
        try
        {
            var channel = await _connection.CreateChannelAsync();

            // Declare exchange
            await channel.ExchangeDeclareAsync(
                exchange: _config.ExchangeName,
                type: ExchangeType.Direct,
                durable: _config.DurableQueues);

            // Declare queue
            await channel.QueueDeclareAsync(
                queue: _config.EmailQueueName,
                durable: _config.DurableQueues,
                exclusive: false,
                autoDelete: false);

            // Bind queue
            await channel.QueueBindAsync(
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
            _channelLock.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ channel");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}