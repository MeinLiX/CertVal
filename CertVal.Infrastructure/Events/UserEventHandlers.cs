using CertVal.Core.Events;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

// User event handlers
public class UserEventHandlers :
    IDomainEventHandler<UserRegisteredEvent>,
    IDomainEventHandler<UserEmailConfirmedEvent>,
    IDomainEventHandler<UserPasswordChangedEvent>
{
    private readonly ILogger<UserEventHandlers> _logger;

    public UserEventHandlers(ILogger<UserEventHandlers> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User registered: {UserId} with email {Email} ({FullName})",
            domainEvent.UserId, domainEvent.Email, domainEvent.FullName);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserEmailConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User email confirmed: {UserId} ({Email})",
            domainEvent.UserId, domainEvent.Email);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserPasswordChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User password changed: {UserId}", domainEvent.UserId);
        return Task.CompletedTask;
    }
}
