using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Events;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CertVal.Infrastructure.Events;

public class EmailNotificationEventHandlers :
    IDomainEventHandler<UserRegisteredEvent>,
    IDomainEventHandler<WorkspaceMemberInvitedEvent>,
    IDomainEventHandler<CertificateExpiringEvent>,
    IDomainEventHandler<CertificateExpiredEvent>
{
    private readonly IEmailNotificationPublisher _emailPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailNotificationEventHandlers> _logger;

    public EmailNotificationEventHandlers(
        IEmailNotificationPublisher emailPublisher,
        IUnitOfWork unitOfWork,
        ILogger<EmailNotificationEventHandlers> logger)
    {
        _emailPublisher = emailPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for UserRegisteredEvent: {UserId}", domainEvent.UserId);
                return;
            }

            if (string.IsNullOrEmpty(user.EmailConfirmationToken))
            {
                _logger.LogWarning("User {UserId} has no email confirmation token", domainEvent.UserId);
                return;
            }

            await _emailPublisher.PublishUserRegisteredAsync(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.EmailConfirmationToken,
                cancellationToken);

            _logger.LogInformation("Published user registration email for user {UserId} ({Email})",
                domainEvent.UserId, domainEvent.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle UserRegisteredEvent for user {UserId}", domainEvent.UserId);
        }
    }

    public async Task HandleAsync(WorkspaceMemberInvitedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(domainEvent.WorkspaceId, cancellationToken);
            var invitedUser = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var inviterUser = await _unitOfWork.Users.GetByIdAsync(domainEvent.InvitedByUserId, cancellationToken);

            if (workspace == null || invitedUser == null || inviterUser == null)
            {
                _logger.LogWarning("Missing entities for WorkspaceMemberInvitedEvent: Workspace={WorkspaceExists}, InvitedUser={InvitedUserExists}, InviterUser={InviterUserExists}",
                    workspace != null, invitedUser != null, inviterUser != null);
                return;
            }

            var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(
                domainEvent.WorkspaceId, domainEvent.UserId, false, cancellationToken);

            if (membership == null)
            {
                _logger.LogWarning("Membership not found for WorkspaceMemberInvitedEvent: WorkspaceId={WorkspaceId}, UserId={UserId}",
                    domainEvent.WorkspaceId, domainEvent.UserId);
                return;
            }

            await _emailPublisher.PublishWorkspaceInvitationAsync(
               workspace.Id,
               invitedUser.FullName,
               inviterUser.FullName,
               workspace.Name,
               domainEvent.InvitationToken,
               membership.Role.ToString(),
               invitedUser.Email,
               cancellationToken);

            _logger.LogInformation("Published workspace invitation email for user {UserId} to workspace {WorkspaceId}",
                domainEvent.UserId, domainEvent.WorkspaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle WorkspaceMemberInvitedEvent for workspace {WorkspaceId}, user {UserId}",
                domainEvent.WorkspaceId, domainEvent.UserId);
        }
    }

    public async Task HandleAsync(CertificateExpiringEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendCertificateNotificationAsync(domainEvent.CertificateId, domainEvent.WorkspaceId,
                domainEvent.Subject, domainEvent.ExpiryDate, domainEvent.DaysUntilExpiry, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CertificateExpiringEvent for certificate {CertificateId}",
                domainEvent.CertificateId);
        }
    }

    public async Task HandleAsync(CertificateExpiredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendCertificateNotificationAsync(domainEvent.CertificateId, domainEvent.WorkspaceId,
                domainEvent.Subject, domainEvent.ExpiryDate, 0, true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CertificateExpiredEvent for certificate {CertificateId}",
                domainEvent.CertificateId);
        }
    }

    private async Task SendCertificateNotificationAsync(Guid certificateId, Guid workspaceId, string subject,
        DateTime expiryDate, int daysUntilExpiry, bool isExpired, CancellationToken cancellationToken)
    {
        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        if (certificate == null) return;

        if (certificate.IsSkipped)
        {
            _logger.LogInformation("Skipping email notification for certificate {CertificateId} as it is marked as skipped", certificateId);
            return;
        }

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return;

        var notificationRules = await _unitOfWork.NotificationRules.GetByWorkspaceAsync(workspaceId, cancellationToken);
        var activeRules = notificationRules
            .Where(r => r.IsEnabled &&
                        r.ChannelType == NotificationChannelType.Email &&
                        r.DaysBeforeExpiry >= daysUntilExpiry)
            .ToList();

        if (!activeRules.Any()) return;

        foreach (var rule in activeRules)
        {
            var lastNotification = await _unitOfWork.NotificationHistory
                   .GetLastNotificationAsync(certificateId, rule.Id, cancellationToken);

            if (!ShouldSendNotification(lastNotification, rule.Frequency))
            {
                continue;
            }

            List<Guid> recipientUserIds;
            try
            {
                var config = JsonSerializer.Deserialize<JsonElement>(rule.ChannelConfig);
                if (config.TryGetProperty("userIds", out var idsElement) && idsElement.ValueKind == JsonValueKind.Array)
                {
                    recipientUserIds = idsElement.EnumerateArray()
                        .Select(e => e.TryGetGuid(out var guid) ? guid : Guid.Empty)
                        .Where(g => g != Guid.Empty).ToList();
                }
                else
                {
                    _logger.LogWarning("Invalid ChannelConfig for email rule {RuleId}: 'userIds' array not found.", rule.Id);
                    continue;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse ChannelConfig for email rule {RuleId}", rule.Id);
                continue;
            }

            if (!recipientUserIds.Any()) continue;

            var recipients = new List<User>();
            foreach (var userId in recipientUserIds)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user != null && user.EmailNotificationsEnabled)
                {
                    recipients.Add(user);
                }
            }

            if (!recipients.Any())
            {
                _logger.LogDebug("No recipients with notifications enabled for rule {RuleId}", rule.Id);
                continue;
            }

            if (rule.RecipientAggregationMode == RecipientAggregationMode.SingleEmailToAll)
            {
                var aggregatedEmails = recipients.Select(r => r.Email).Distinct().ToList();
                try
                {
                    var notification = NotificationHistory.Create(
                        rule.Id, certificateId, NotificationChannelType.Email,
                        string.Join(",", aggregatedEmails),
                        GenerateEmailSubject(certificate, daysUntilExpiry, isExpired),
                        GenerateEmailMessage(certificate, workspace, daysUntilExpiry, isExpired),
                        DateTime.UtcNow
                    );

                    await _unitOfWork.NotificationHistory.AddAsync(notification, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _emailPublisher.PublishCertificateExpiringAggregatedAsync(
                        aggregatedEmails,
                        workspace.Name,
                        certificate.Subject,
                        certificate.Issuer,
                        expiryDate,
                        daysUntilExpiry,
                        cancellationToken);

                    notification.MarkAsSent();
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Sent aggregated certificate notification for certificate {CertificateId} to {Count} recipients via rule {RuleId}",
                        certificateId, aggregatedEmails.Count, rule.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send aggregated certificate notification for certificate {CertificateId}", certificateId);
                    var failedNotification = await _unitOfWork.NotificationHistory
                        .GetLastNotificationAsync(certificateId, rule.Id, cancellationToken);
                    if (failedNotification != null && failedNotification.Status == NotificationStatus.Pending)
                    {
                        failedNotification.MarkAsFailed(ex.Message);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else
            {
                foreach (var user in recipients)
                {
                    try
                    {
                        var notification = NotificationHistory.Create(
                            rule.Id, certificateId, NotificationChannelType.Email,
                            user.Email,
                            GenerateEmailSubject(certificate, daysUntilExpiry, isExpired),
                            GenerateEmailMessage(certificate, workspace, daysUntilExpiry, isExpired),
                            DateTime.UtcNow
                        );

                        await _unitOfWork.NotificationHistory.AddAsync(notification, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        await _emailPublisher.PublishCertificateExpiringAsync(
                            user.Email,
                            workspace.Name,
                            certificate.Subject,
                            certificate.Issuer,
                            expiryDate,
                            daysUntilExpiry,
                            cancellationToken);

                        notification.MarkAsSent();
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Sent certificate notification for certificate {CertificateId} to {Email} via rule {RuleId}",
                           certificateId, user.Email, rule.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send certificate notification for certificate {CertificateId} to {Email}",
                            certificateId, user.Email);

                        var failedNotification = await _unitOfWork.NotificationHistory
                            .GetLastNotificationAsync(certificateId, rule.Id, cancellationToken);
                        if (failedNotification != null && failedNotification.Status == NotificationStatus.Pending)
                        {
                            failedNotification.MarkAsFailed(ex.Message);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }
                    }
                }
            }
        }
    }

    private static bool ShouldSendNotification(NotificationHistory? lastNotification, NotificationFrequency frequency)
    {
        if (lastNotification == null)
            return true;

        if (lastNotification.Status == NotificationStatus.Failed && lastNotification.CanRetry)
            return true;

        if (lastNotification.Status is NotificationStatus.Sent or NotificationStatus.Delivered)
        {
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

        if (lastNotification.Status == NotificationStatus.Pending &&
            DateTime.UtcNow - lastNotification.CreatedAt > TimeSpan.FromHours(1))
            return true;

        return false;
    }

    private static string GenerateEmailSubject(Certificate certificate, int daysUntilExpiry, bool isExpired)
    {
        var urgencyLevel = (isExpired, daysUntilExpiry) switch
        {
            (true, _) => "🔴 EXPIRED",
            (false, <= 7) => "🟠 CRITICAL",
            (false, <= 30) => "🟡 WARNING",
            _ => "🔵 INFO"
        };

        var timeText = isExpired ? "has expired" : $"expires in {daysUntilExpiry} days";

        return $"{urgencyLevel} Certificate Alert: {certificate.Subject} {timeText}";
    }

    private static string GenerateEmailMessage(Certificate certificate, Workspace workspace, int daysUntilExpiry, bool isExpired)
    {
        var status = isExpired ? "has expired" : $"expires in {daysUntilExpiry} day(s)";
        var urgency = isExpired ? "CRITICAL" : (daysUntilExpiry <= 7 ? "URGENT" : "Action Required");

        return $@"
<html>
<body>
<h2>Certificate Expiry Alert</h2>
<p><strong>{urgency}:</strong> The following certificate in workspace <strong>{workspace.Name}</strong> {status}:</p>

<table border='1' cellpadding='8' cellspacing='0' style='border-collapse: collapse;'>
<tr><td><strong>Subject:</strong></td><td>{certificate.Subject}</td></tr>
<tr><td><strong>Issuer:</strong></td><td>{certificate.Issuer}</td></tr>
<tr><td><strong>Serial Number:</strong></td><td>{certificate.SerialNumber ?? "N/A"}</td></tr>
<tr><td><strong>Expiry Date:</strong></td><td>{certificate.NotAfter:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
<tr><td><strong>File:</strong></td><td>{certificate.OriginalFileName}</td></tr>
<tr><td><strong>Days Until Expiry:</strong></td><td>{daysUntilExpiry}</td></tr>
</table>

<p><strong>Action Required:</strong> Please renew this certificate as soon as possible to avoid service disruption.</p>

<p>Best regards,<br/>CertVal Monitoring System</p>
</body>
</html>";
    }
}