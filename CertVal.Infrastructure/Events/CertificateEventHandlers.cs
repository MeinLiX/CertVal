using CertVal.Core.Events;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class CertificateEventHandlers :
    IDomainEventHandler<CertificateUploadedEvent>,
    IDomainEventHandler<CertificateExpiringEvent>,
    IDomainEventHandler<CertificateExpiredEvent>,
    IDomainEventHandler<CertificateBundleProcessedEvent>
{
    private readonly ILogger<CertificateEventHandlers> _logger;

    public CertificateEventHandlers(ILogger<CertificateEventHandlers> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CertificateUploadedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Certificate uploaded: {CertificateId} in workspace {WorkspaceId}, Subject: '{Subject}', Expires: {ExpiryDate}",
            domainEvent.CertificateId, domainEvent.WorkspaceId, domainEvent.Subject, domainEvent.ExpiryDate);
        return Task.CompletedTask;
    }

    public Task HandleAsync(CertificateExpiringEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Certificate expiring: {CertificateId} '{Subject}' expires in {DaysUntilExpiry} days ({ExpiryDate})",
            domainEvent.CertificateId, domainEvent.Subject, domainEvent.DaysUntilExpiry, domainEvent.ExpiryDate);
        return Task.CompletedTask;
    }

    public Task HandleAsync(CertificateExpiredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Certificate expired: {CertificateId} '{Subject}' expired on {ExpiryDate}",
            domainEvent.CertificateId, domainEvent.Subject, domainEvent.ExpiryDate);
        return Task.CompletedTask;
    }

    public Task HandleAsync(CertificateBundleProcessedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Certificate bundle processed: {ParentCertificateId} in workspace {WorkspaceId}, contains {CertificateCount} certificates",
            domainEvent.ParentCertificateId, domainEvent.WorkspaceId, domainEvent.CertificateCount);
        return Task.CompletedTask;
    }
}
