using CertVal.Application.Services;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
                await GenerateNotificationsAsync(stoppingToken);

                _logger.LogDebug("Notification processing completed. Next check in {Interval}", _checkInterval);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during notification processing");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await notificationService.ProcessPendingNotificationsAsync(cancellationToken);
    }

    private async Task GenerateNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Get all active notification rules
        var activeRules = await unitOfWork.NotificationRules.GetActiveRulesAsync(cancellationToken);

        foreach (var rule in activeRules)
        {
            try
            {
                await GenerateNotificationsForRuleAsync(rule, unitOfWork, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating notifications for rule {RuleId}", rule.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task GenerateNotificationsForRuleAsync(
        NotificationRule rule,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        // Get certificates that match the rule criteria
        var targetDate = DateTime.UtcNow.AddDays(rule.DaysBeforeExpiry);
        var certificates = await unitOfWork.Certificates.GetByWorkspaceAsync(rule.WorkspaceId, cancellationToken);

        var expiringCertificates = certificates.Where(c =>
            c.NotAfter.Date == targetDate.Date &&
            c.NotAfter > DateTime.UtcNow).ToList();

        foreach (var certificate in expiringCertificates)
        {
            // Check if notification already sent for this certificate/rule combination
            var lastNotification = await unitOfWork.NotificationHistory
                .GetLastNotificationAsync(certificate.Id, rule.Id, cancellationToken);

            var shouldSendNotification = ShouldSendNotification(lastNotification, rule.Frequency);

            if (shouldSendNotification)
            {
                await CreateNotificationAsync(rule, certificate, unitOfWork, cancellationToken);
            }
        }
    }

    private bool ShouldSendNotification(NotificationHistory? lastNotification, NotificationFrequency frequency)
    {
        if (lastNotification == null)
            return true;

        if (lastNotification.Status == NotificationStatus.Failed && lastNotification.CanRetry)
            return true;

        if (frequency == NotificationFrequency.Once)
            return false;

        var daysSinceLastNotification = (DateTime.UtcNow - lastNotification.CreatedAt).Days;

        return frequency switch
        {
            NotificationFrequency.Daily => daysSinceLastNotification >= 1,
            NotificationFrequency.Weekly => daysSinceLastNotification >= 7,
            NotificationFrequency.Monthly => daysSinceLastNotification >= 30,
            _ => false
        };
    }

    private async Task CreateNotificationAsync(
        NotificationRule rule,
        Certificate certificate,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var workspace = await unitOfWork.Workspaces.GetByIdAsync(rule.WorkspaceId, cancellationToken);
        if (workspace == null) return;

        var daysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).Days;

        var recipient = GetRecipientFromChannelConfig(rule.ChannelConfig, workspace.Owner.Email);
        var subject = GenerateSubject(certificate, daysUntilExpiry);
        var message = GenerateMessage(certificate, workspace, daysUntilExpiry);

        var notification = NotificationHistory.Create(
            rule.Id,
            certificate.Id,
            rule.ChannelType,
            recipient,
            subject,
            message,
            DateTime.UtcNow
        );

        await unitOfWork.NotificationHistory.AddAsync(notification, cancellationToken);

        _logger.LogInformation("Created notification for certificate {CertificateId} expiring in {Days} days",
            certificate.Id, daysUntilExpiry);
    }

    private string GetRecipientFromChannelConfig(string channelConfig, string defaultEmail)
    {
        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(channelConfig);
            if (config?.TryGetValue("email", out var email) == true)
                return email.ToString() ?? defaultEmail;
        }
        catch
        {
            // Fallback to default
        }

        return defaultEmail;
    }

    private string GenerateSubject(Certificate certificate, int daysUntilExpiry)
    {
        var urgencyLevel = daysUntilExpiry switch
        {
            <= 0 => "🔴 EXPIRED",
            <= 7 => "🟠 CRITICAL",
            <= 30 => "🟡 WARNING",
            _ => "🔵 INFO"
        };

        return $"{urgencyLevel} Certificate Alert: {certificate.Subject} expires in {daysUntilExpiry} days";
    }

    private string GenerateMessage(Certificate certificate, Workspace workspace, int daysUntilExpiry)
    {
        var status = daysUntilExpiry <= 0 ? "has expired" : $"expires in {daysUntilExpiry} day(s)";

        return $@"
<html>
<body>
<h2>Certificate Expiry Alert</h2>
<p>The following certificate in workspace <strong>{workspace.Name}</strong> {status}:</p>

<table border='1' cellpadding='5' cellspacing='0'>
<tr><td><strong>Subject:</strong></td><td>{certificate.Subject}</td></tr>
<tr><td><strong>Issuer:</strong></td><td>{certificate.Issuer}</td></tr>
<tr><td><strong>Serial Number:</strong></td><td>{certificate.SerialNumber}</td></tr>
<tr><td><strong>Expiry Date:</strong></td><td>{certificate.NotAfter:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
<tr><td><strong>File:</strong></td><td>{certificate.OriginalFileName}</td></tr>
</table>

<p><strong>Action Required:</strong> Please renew this certificate as soon as possible to avoid service disruption.</p>

<p>Best regards,<br/>CertVal Monitoring System</p>
</body>
</html>";
    }
}