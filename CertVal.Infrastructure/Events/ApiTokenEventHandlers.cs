using CertVal.Core.Events;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class ApiTokenEventHandlers :
    IDomainEventHandler<ApiTokenCreatedEvent>,
    IDomainEventHandler<ApiTokenUsedEvent>,
    IDomainEventHandler<ApiTokenRevokedEvent>
{
    private readonly ILogger<ApiTokenEventHandlers> _logger;

    public ApiTokenEventHandlers(ILogger<ApiTokenEventHandlers> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ApiTokenCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API token created: {TokenId} '{Name}' for user {UserId} with scope {Scope}",
            domainEvent.TokenId, domainEvent.Name, domainEvent.UserId, domainEvent.Scope);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ApiTokenUsedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("API token used: {TokenId} by user {UserId} from IP {IpAddress}",
            domainEvent.TokenId, domainEvent.UserId, domainEvent.IpAddress ?? "unknown");
        return Task.CompletedTask;
    }

    public Task HandleAsync(ApiTokenRevokedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("API token revoked: {TokenId} for user {UserId}",
            domainEvent.TokenId, domainEvent.UserId);
        return Task.CompletedTask;
    }
}