using CertVal.Core.Events;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class WorkspaceEventHandlers :
    IDomainEventHandler<WorkspaceCreatedEvent>,
    IDomainEventHandler<WorkspaceUpdatedEvent>,
    IDomainEventHandler<WorkspaceOwnershipTransferredEvent>,
    IDomainEventHandler<WorkspaceMemberInvitedEvent>,
    IDomainEventHandler<WorkspaceMemberJoinedEvent>,
    IDomainEventHandler<WorkspaceMemberRemovedEvent>
{
    private readonly ILogger<WorkspaceEventHandlers> _logger;

    public WorkspaceEventHandlers(ILogger<WorkspaceEventHandlers> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(WorkspaceCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Workspace created: {WorkspaceId} '{Name}' by owner {OwnerId}",
            domainEvent.WorkspaceId, domainEvent.Name, domainEvent.OwnerId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkspaceUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Workspace updated: {WorkspaceId} renamed to '{Name}'",
            domainEvent.WorkspaceId, domainEvent.Name);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkspaceOwnershipTransferredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Workspace ownership transferred: {WorkspaceId} from {OldOwnerId} to {NewOwnerId}",
            domainEvent.WorkspaceId, domainEvent.OldOwnerId, domainEvent.NewOwnerId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkspaceMemberInvitedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} invited to workspace {WorkspaceId} by {InvitedByUserId}",
            domainEvent.UserId, domainEvent.WorkspaceId, domainEvent.InvitedByUserId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkspaceMemberJoinedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} joined workspace {WorkspaceId}",
            domainEvent.UserId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(WorkspaceMemberRemovedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} removed from workspace {WorkspaceId}",
            domainEvent.UserId, domainEvent.WorkspaceId);
        return Task.CompletedTask;
    }
}