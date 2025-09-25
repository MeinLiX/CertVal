using CertVal.Application.Common.Interfaces;
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
            r.ChannelType == Core.Enums.NotificationChannelType.Webhook &&
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
            var webhookConfig = ParseWebhookConfig(rule.ChannelConfig);
            if (webhookConfig?.Url == null)
            {
                _logger.LogWarning("Invalid webhook configuration for rule {RuleId}", rule.Id);
                return;
            }

            var (valid, uri, error) = await _security.ValidateUrlAsync(webhookConfig.Url, cancellationToken);
            if (!valid || uri == null)
            {
                _logger.LogWarning("Webhook URL rejected for rule {RuleId}: {Error}", rule.Id, error);
                return;
            }

            var subject = GenerateSubject(certificate, daysUntilExpiry);
            var message = GenerateMessage(certificate, workspace, daysUntilExpiry);

            var notificationHistory = Core.Entities.NotificationHistory.Create(
                rule.Id, certificate.Id, rule.ChannelType,
                uri.ToString(), subject, message, DateTime.UtcNow);

            await _unitOfWork.NotificationHistory.AddAsync(notificationHistory, cancellationToken);

            var success = await SendWebhookAsync(uri, webhookConfig, certificate, workspace, daysUntilExpiry, cancellationToken);

            if (success)
            {
                notificationHistory.MarkAsSent();
                _logger.LogDebug("Successfully sent webhook notification for rule {RuleId}", rule.Id);
            }
            else
            {
                notificationHistory.MarkAsFailed("Webhook request failed");
                _logger.LogWarning("Webhook notification failed for rule {RuleId}", rule.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook rule {RuleId}", rule.Id);
        }
    }

    private async Task<bool> SendWebhookAsync(
        Uri uri,
        WebhookConfig config,
        Core.Entities.Certificate certificate,
        Core.Entities.Workspace workspace,
        int daysUntilExpiry,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("webhook");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

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

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var headers = _security.SanitizeHeaders(config.Headers);
            foreach (var (key, value) in headers)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }

            var response = await httpClient.PostAsync(uri, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to {Url}", uri);
            return false;
        }
    }

    private WebhookConfig? ParseWebhookConfig(string channelConfig)
    {
        try
        {
            return JsonSerializer.Deserialize<WebhookConfig>(channelConfig, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse webhook config: {Config}", channelConfig);
            return null;
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

    private record WebhookConfig(string? Url, Dictionary<string, string>? Headers = null);
}