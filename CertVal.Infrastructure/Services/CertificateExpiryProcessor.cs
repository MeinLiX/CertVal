using CertVal.Application.Common.Interfaces;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CertVal.Infrastructure.Services;

/// <summary>
/// Orchestrates per-workspace certificate expiry handling:
///   1. Removes expired certificates older than the retention window when the workspace opts-in.
///   2. Groups expiring/expired certificates per workspace and sends a single consolidated
///      digest email per matching notification rule (instead of one email per certificate).
/// Per-certificate webhook notifications continue to fire through the domain event pipeline.
/// </summary>
public sealed class CertificateExpiryProcessor : ICertificateExpiryProcessor
{
    private const int ExpiryLookaheadDays = 90;
    private const int DigestMaxItems = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationPublisher _emailPublisher;
    private readonly ICertificateStorageService _storageService;
    private readonly ILogger<CertificateExpiryProcessor> _logger;

    public CertificateExpiryProcessor(
        IUnitOfWork unitOfWork,
        IEmailNotificationPublisher emailPublisher,
        ICertificateStorageService storageService,
        ILogger<CertificateExpiryProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _emailPublisher = emailPublisher;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task ProcessExpiryAsync(CancellationToken cancellationToken = default)
    {
        var expiring = (await _unitOfWork.Certificates
            .GetExpiringAsync(ExpiryLookaheadDays, cancellationToken))
            .ToList();

        var expired = (await _unitOfWork.Certificates
            .GetExpiredAsync(cancellationToken))
            .ToList();

        var byWorkspace = expiring.Concat(expired)
            .GroupBy(c => c.WorkspaceId);

        foreach (var group in byWorkspace)
        {
            try
            {
                await ProcessWorkspaceAsync(group.Key, group.ToList(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expiry for workspace {WorkspaceId}", group.Key);
            }
        }
    }

    private async Task ProcessWorkspaceAsync(Guid workspaceId, List<Certificate> candidates, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return;

        var retained = await AutoDeleteExpiredAsync(workspace, candidates, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var cert in retained)
        {
            var eventsBefore = cert.DomainEvents.Count;
            cert.CheckExpiry();

            if (cert.IsExpired && cert.Status != CertificateStatus.Expired)
                cert.MarkAsExpired();

            if (cert.DomainEvents.Count != eventsBefore)
                _logger.LogDebug("Raised expiry events for certificate {CertificateId}", cert.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SendDigestAsync(workspace, retained, cancellationToken);
    }

    private async Task<List<Certificate>> AutoDeleteExpiredAsync(
        Workspace workspace,
        List<Certificate> candidates,
        CancellationToken cancellationToken)
    {
        if (!workspace.AutoDeleteExpiredCertificates)
            return candidates;

        var cutoff = DateTime.UtcNow.AddDays(-Workspace.ExpiredCertificateRetentionDays);
        var toDelete = candidates.Where(c => c.NotAfter < cutoff).ToList();

        if (toDelete.Count == 0) return candidates;

        foreach (var cert in toDelete)
        {
            try
            {
                await _unitOfWork.Certificates.DeleteAsync(cert.Id, cancellationToken);

                if (!string.IsNullOrEmpty(cert.FilePath))
                {
                    try
                    {
                        await _storageService.DeleteCertificateAsync(cert.FilePath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to delete storage object {ObjectKey} for auto-deleted certificate {CertificateId}",
                            cert.FilePath, cert.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-delete certificate {CertificateId}", cert.Id);
            }
        }

        _logger.LogInformation(
            "Auto-deleted {Count} certificate(s) expired for more than {Days} days in workspace {WorkspaceId}",
            toDelete.Count, Workspace.ExpiredCertificateRetentionDays, workspace.Id);

        var deletedIds = toDelete.Select(c => c.Id).ToHashSet();
        return candidates.Where(c => !deletedIds.Contains(c.Id)).ToList();
    }

    private async Task SendDigestAsync(Workspace workspace, List<Certificate> candidates, CancellationToken cancellationToken)
    {
        var active = candidates.Where(c => !c.IsSkipped).ToList();
        if (active.Count == 0) return;

        var rules = (await _unitOfWork.NotificationRules.GetByWorkspaceAsync(workspace.Id, cancellationToken))
            .Where(r => r.IsEnabled && r.ChannelType == NotificationChannelType.Email)
            .ToList();

        if (rules.Count == 0) return;

        foreach (var rule in rules)
        {
            try
            {
                await SendDigestForRuleAsync(workspace, rule, active, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send digest for rule {RuleId} in workspace {WorkspaceId}",
                    rule.Id, workspace.Id);
            }
        }
    }

    private async Task SendDigestForRuleAsync(
        Workspace workspace,
        NotificationRule rule,
        IReadOnlyList<Certificate> candidates,
        CancellationToken cancellationToken)
    {
        var eligibleCerts = new List<Certificate>();
        var pendingHistory = new List<NotificationHistory>();

        foreach (var cert in candidates)
        {
            var days = (int)(cert.NotAfter - DateTime.UtcNow).TotalDays;
            if (!cert.IsExpired && rule.DaysBeforeExpiry < days)
                continue;

            var last = await _unitOfWork.NotificationHistory
                .GetLastNotificationAsync(cert.Id, rule.Id, cancellationToken);

            if (!ShouldSendNotification(last, rule.Frequency))
                continue;

            eligibleCerts.Add(cert);
        }

        if (eligibleCerts.Count == 0) return;

        var recipientEmails = await ResolveRecipientEmailsAsync(rule, cancellationToken);
        if (recipientEmails.Count == 0) return;

        eligibleCerts = eligibleCerts.OrderBy(c => c.NotAfter).ToList();
        var shown = eligibleCerts.Take(DigestMaxItems).ToList();
        var remaining = Math.Max(0, eligibleCerts.Count - shown.Count);

        var items = shown.Select(c =>
        {
            var days = (int)(c.NotAfter - DateTime.UtcNow).TotalDays;
            return new CertificateExpiryDigestItem
            {
                Subject = c.Subject,
                Issuer = c.Issuer,
                SerialNumber = c.SerialNumber,
                ExpiryDate = c.NotAfter,
                DaysUntilExpiry = Math.Max(0, days),
                IsExpired = c.IsExpired
            };
        }).ToList();

        var digest = new CertificateExpiryDigestMessage
        {
            WorkspaceName = workspace.Name,
            Recipients = recipientEmails,
            Items = items,
            TotalCount = eligibleCerts.Count,
            RemainingCount = remaining,
            ExpiredCount = eligibleCerts.Count(c => c.IsExpired),
            ExpiringCount = eligibleCerts.Count(c => !c.IsExpired),
            CorrelationId = rule.Id.ToString()
        };

        foreach (var cert in eligibleCerts)
        {
            var history = NotificationHistory.Create(
                rule.Id, cert.Id, NotificationChannelType.Email,
                string.Join(",", recipientEmails),
                $"Certificate expiry digest - {workspace.Name}",
                $"Digest batch containing {eligibleCerts.Count} certificate(s).",
                DateTime.UtcNow);
            pendingHistory.Add(history);
            await _unitOfWork.NotificationHistory.AddAsync(history, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailPublisher.PublishCertificateExpiryDigestAsync(digest, cancellationToken);

            foreach (var h in pendingHistory) h.MarkAsSent();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sent expiry digest: workspace={WorkspaceId} rule={RuleId} recipients={Recipients} certs={Certs} (shown={Shown}, remaining={Remaining})",
                workspace.Id, rule.Id, recipientEmails.Count, eligibleCerts.Count, shown.Count, remaining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed publishing digest for rule {RuleId} in workspace {WorkspaceId}",
                rule.Id, workspace.Id);

            foreach (var h in pendingHistory) h.MarkAsFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<string>> ResolveRecipientEmailsAsync(NotificationRule rule, CancellationToken cancellationToken)
    {
        var recipientUserIds = ParseUserIds(rule);
        if (recipientUserIds.Count == 0) return new List<string>();

        var emails = new List<string>();
        foreach (var userId in recipientUserIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user is { EmailNotificationsEnabled: true })
                emails.Add(user.Email);
        }
        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<Guid> ParseUserIds(NotificationRule rule)
    {
        try
        {
            var config = JsonSerializer.Deserialize<JsonElement>(rule.ChannelConfig);
            if (config.ValueKind != JsonValueKind.Object ||
                !config.TryGetProperty("userIds", out var idsElement) ||
                idsElement.ValueKind != JsonValueKind.Array)
            {
                return new List<Guid>();
            }

            return idsElement.EnumerateArray()
                .Select(e => e.TryGetGuid(out var guid) ? guid : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid ChannelConfig JSON for rule {RuleId}", rule.Id);
            return new List<Guid>();
        }
    }

    private static bool ShouldSendNotification(NotificationHistory? last, NotificationFrequency frequency)
    {
        if (last == null) return true;

        if (last.Status == NotificationStatus.Failed && last.CanRetry) return true;

        if (last.Status is NotificationStatus.Sent or NotificationStatus.Delivered)
        {
            if (frequency == NotificationFrequency.Once) return false;

            var days = (DateTime.UtcNow - last.CreatedAt).Days;
            return frequency switch
            {
                NotificationFrequency.Daily => days >= 1,
                NotificationFrequency.Weekly => days >= 7,
                NotificationFrequency.Monthly => days >= 30,
                _ => false
            };
        }

        return last.Status == NotificationStatus.Pending &&
               DateTime.UtcNow - last.CreatedAt > TimeSpan.FromHours(1);
    }
}
