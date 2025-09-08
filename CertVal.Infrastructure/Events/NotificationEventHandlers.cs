using CertVal.Core.Events;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class NotificationEventHandlers :
    IDomainEventHandler<NotificationRuleCreatedEvent>,
    IDomainEventHandler<NotificationSentEvent>,
    IDomainEventHandler<NotificationFailedEvent>
{
    private readonly ILogger<NotificationEventHandlers> _logger;

    public NotificationEventHandlers(ILogger<NotificationEventHandlers> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(NotificationRuleCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Notification rule created: {RuleId} '{Name}' in workspace {WorkspaceId}, triggers {DaysBeforeExpiry} days before expiry",
            domainEvent.RuleId, domainEvent.Name, domainEvent.WorkspaceId, domainEvent.DaysBeforeExpiry);
        return Task.CompletedTask;
    }

    public Task HandleAsync(NotificationSentEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Notification sent: {NotificationId} for certificate {CertificateId} to {Recipient} via {Channel}",
            domainEvent.NotificationId, domainEvent.CertificateId, domainEvent.Recipient, domainEvent.Channel);
        return Task.CompletedTask;
    }

    public Task HandleAsync(NotificationFailedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Notification failed: {NotificationId} for certificate {CertificateId} to {Recipient}. Error: {Error}",
            domainEvent.NotificationId, domainEvent.CertificateId, domainEvent.Recipient, domainEvent.Error);
        return Task.CompletedTask;
    }
}
