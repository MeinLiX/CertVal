using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Notifications;
using CertVal.Core.Enums;
using CertVal.Core.Events;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CertVal.Infrastructure.Events;

public class WebhookNotificationEventHandlers :
    IDomainEventHandler<CertificateExpiringEvent>,
    IDomainEventHandler<CertificateExpiredEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationEventHandlers> _logger;
    private readonly IWebhookSecurityService _security;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public WebhookNotificationEventHandlers(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IWebhookSecurityService security,
        ILogger<WebhookNotificationEventHandlers> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _security = security ?? throw new ArgumentNullException(nameof(security));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(CertificateExpiringEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ProcessWebhookNotificationsAsync(
            domainEvent.CertificateId, domainEvent.WorkspaceId,
            domainEvent.DaysUntilExpiry, false, cancellationToken);
    }

    public async Task HandleAsync(CertificateExpiredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ProcessWebhookNotificationsAsync(
            domainEvent.CertificateId, domainEvent.WorkspaceId,
            0, true, cancellationToken);
    }

    private async Task ProcessWebhookNotificationsAsync(
        Guid certificateId, Guid workspaceId, int daysUntilExpiry,
        bool isExpired, CancellationToken cancellationToken)
    {
        try
        {
            var (certificate, workspace) = await LoadEntitiesAsync(certificateId, workspaceId, cancellationToken);

            if (certificate == null || workspace == null)
            {
                _logger.LogWarning("Certificate or workspace not found for webhook notification");
                return;
            }

            if (certificate.IsSkipped)
            {
                _logger.LogInformation("Skipping webhook notification for certificate {CertificateId} as it is marked as skipped", certificateId);
                return;
            }

            var webhookRules = await GetActiveWebhookRulesAsync(workspaceId, daysUntilExpiry, cancellationToken);
            var processedCount = 0;

            foreach (var rule in webhookRules)
            {
                if (await ShouldSendNotificationAsync(certificateId, rule.Id, rule.Frequency, cancellationToken))
                {
                    await ProcessWebhookRuleAsync(rule, certificate, workspace, daysUntilExpiry, cancellationToken);
                    processedCount++;
                }
            }

            if (processedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed {Count} webhook notifications for certificate {Subject}",
                    processedCount, certificate.Subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook notifications for certificate {CertificateId}", certificateId);
        }
    }

    private async Task<(Core.Entities.Certificate?, Core.Entities.Workspace?)> LoadEntitiesAsync(
    Guid certificateId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);

        return (certificate, workspace);
    }

    private async Task<List<Core.Entities.NotificationRule>> GetActiveWebhookRulesAsync(
        Guid workspaceId, int daysUntilExpiry, CancellationToken cancellationToken)
    {
        var allRules = await _unitOfWork.NotificationRules.GetByWorkspaceAsync(workspaceId, cancellationToken);

        return allRules.Where(r =>
            r.IsEnabled &&
            (r.ChannelType == NotificationChannelType.Webhook ||
             r.ChannelType == NotificationChannelType.Slack ||
             r.ChannelType == NotificationChannelType.Telegram) &&
            r.DaysBeforeExpiry >= daysUntilExpiry).ToList();
    }

    private async Task<bool> ShouldSendNotificationAsync(
        Guid certificateId, Guid ruleId, Core.Enums.NotificationFrequency frequency, CancellationToken cancellationToken)
    {
        var lastNotification = await _unitOfWork.NotificationHistory
            .GetLastNotificationAsync(certificateId, ruleId, cancellationToken);

        if (lastNotification == null) return true;

        if (lastNotification.Status == Core.Enums.NotificationStatus.Failed && lastNotification.CanRetry)
            return true;

        if (frequency == Core.Enums.NotificationFrequency.Once &&
            lastNotification.Status == Core.Enums.NotificationStatus.Sent)
            return false;

        var daysSinceLastNotification = (DateTime.UtcNow - lastNotification.CreatedAt).Days;

        return frequency switch
        {
            Core.Enums.NotificationFrequency.Daily => daysSinceLastNotification >= 1,
            Core.Enums.NotificationFrequency.Weekly => daysSinceLastNotification >= 7,
            Core.Enums.NotificationFrequency.Monthly => daysSinceLastNotification >= 30,
            _ => false
        };
    }

    private async Task ProcessWebhookRuleAsync(
        Core.Entities.NotificationRule rule,
        Core.Entities.Certificate certificate,
        Core.Entities.Workspace workspace,
        int daysUntilExpiry,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = GenerateSubject(certificate, daysUntilExpiry);
            var message = GenerateMessage(certificate, workspace, daysUntilExpiry);

            var delivery = await PrepareDeliveryAsync(rule, certificate, workspace, daysUntilExpiry, subject, message, cancellationToken);
            if (!delivery.Ok || delivery.Uri is null)
            {
                _logger.LogWarning("Notification delivery could not be prepared for rule {RuleId}: {Error}", rule.Id, delivery.Error);
                return;
            }

            var notificationHistory = Core.Entities.NotificationHistory.Create(
                rule.Id, certificate.Id, rule.ChannelType,
                delivery.Uri.ToString(), subject, message, DateTime.UtcNow);

            await _unitOfWork.NotificationHistory.AddAsync(notificationHistory, cancellationToken);

            var success = await SendAsync(delivery.Uri, delivery.Payload, delivery.Headers, cancellationToken);

            if (success)
            {
                notificationHistory.MarkAsSent();
                _logger.LogDebug("Successfully sent {Channel} notification for rule {RuleId}", rule.ChannelType, rule.Id);
            }
            else
            {
                notificationHistory.MarkAsFailed($"{rule.ChannelType} request failed");
                _logger.LogWarning("{Channel} notification failed for rule {RuleId}", rule.ChannelType, rule.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification rule {RuleId}", rule.Id);
        }
    }

    private async Task<(bool Ok, Uri? Uri, string Payload, Dictionary<string, string>? Headers, string? Error)> PrepareDeliveryAsync(
        Core.Entities.NotificationRule rule,
        Core.Entities.Certificate certificate,
        Core.Entities.Workspace workspace,
        int daysUntilExpiry,
        string subject,
        string message,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(rule.ChannelConfig);
        var root = doc.RootElement;

        switch (rule.ChannelType)
        {
            case NotificationChannelType.Webhook:
            {
                if (!root.TryGetProperty("url", out var urlEl) || string.IsNullOrWhiteSpace(urlEl.GetString()))
                    return (false, null, string.Empty, null, "Webhook URL missing");

                var (valid, uri, error) = await _security.ValidateUrlAsync(urlEl.GetString(), cancellationToken);
                if (!valid || uri is null) return (false, null, string.Empty, null, error);

                Dictionary<string, string>? headers = null;
                if (root.TryGetProperty("headers", out var hEl) && hEl.ValueKind == JsonValueKind.Object)
                {
                    var temp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in hEl.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String) temp[p.Name] = p.Value.GetString() ?? string.Empty;
                    headers = _security.SanitizeHeaders(temp).ToDictionary(k => k.Key, v => v.Value);
                }

                var payload = BuildWebhookPayload(certificate, workspace, daysUntilExpiry);
                return (true, uri, payload, headers, null);
            }

            case NotificationChannelType.Slack:
            {
                if (!root.TryGetProperty("webhookUrl", out var urlEl) || string.IsNullOrWhiteSpace(urlEl.GetString()))
                    return (false, null, string.Empty, null, "Slack webhookUrl missing");

                var (valid, uri, error) = await _security.ValidateUrlAsync(urlEl.GetString(), cancellationToken);
                if (!valid || uri is null) return (false, null, string.Empty, null, error);

                return (true, uri, ChatNotificationFormatter.BuildSlackPayload(subject, message), null, null);
            }

            case NotificationChannelType.Telegram:
            {
                var botToken = root.TryGetProperty("botToken", out var tEl) ? tEl.GetString() : null;
                var chatId = root.TryGetProperty("chatId", out var cEl) ? cEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                    return (false, null, string.Empty, null, "Telegram botToken/chatId missing");

                var url = ChatNotificationFormatter.BuildTelegramUrl(botToken);
                var (valid, uri, error) = await _security.ValidateUrlAsync(url, cancellationToken);
                if (!valid || uri is null) return (false, null, string.Empty, null, error);

                return (true, uri, ChatNotificationFormatter.BuildTelegramPayload(chatId, subject, message), null, null);
            }

            default:
                return (false, null, string.Empty, null, "Unsupported channel");
        }
    }

    private string BuildWebhookPayload(
        Core.Entities.Certificate certificate,
        Core.Entities.Workspace workspace,
        int daysUntilExpiry)
    {
        var payload = new
        {
            EventType = daysUntilExpiry <= 0 ? "CertificateExpired" : "CertificateExpiring",
            Timestamp = DateTime.UtcNow,
            Certificate = new
            {
                certificate.Id,
                Subject = _security.SanitizeValue(certificate.Subject, 256),
                Issuer = _security.SanitizeValue(certificate.Issuer, 256),
                SerialNumber = _security.SanitizeValue(certificate.SerialNumber, 128),
                certificate.NotAfter,
                DaysUntilExpiry = daysUntilExpiry,
                IsExpired = daysUntilExpiry <= 0,
                FileName = _security.SanitizeValue(certificate.OriginalFileName, 128)
            },
            Workspace = new
            {
                workspace.Id,
                Name = _security.SanitizeValue(workspace.Name, 200),
                Owner = _security.SanitizeValue(workspace.Owner.Email, 200)
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private async Task<bool> SendAsync(
        Uri uri,
        string payloadJson,
        Dictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("webhook");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }

            var response = await httpClient.PostAsync(uri, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to {Url}", uri);
            return false;
        }
    }

    private static string GenerateSubject(Core.Entities.Certificate certificate, int daysUntilExpiry)
    {
        var status = daysUntilExpiry <= 0 ? "expired" : $"expires in {daysUntilExpiry} days";
        return $"Certificate {status}: {certificate.Subject}";
    }

    private static string GenerateMessage(Core.Entities.Certificate certificate, Core.Entities.Workspace workspace, int daysUntilExpiry)
    {
        var status = daysUntilExpiry <= 0 ? "has expired" : $"expires in {daysUntilExpiry} day(s)";
        return $"Certificate '{certificate.Subject}' in workspace '{workspace.Name}' {status}. Please renew immediately.";
    }
}