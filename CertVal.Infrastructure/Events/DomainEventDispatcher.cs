using CertVal.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public interface IDomainEventDispatcher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Publishing domain event: {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug("Successfully published domain event: {EventType} with {HandlerCount} handlers",
                domainEvent.GetType().Name, tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any())
        {
            return;
        }

        _logger.LogDebug("Publishing {EventCount} domain events", events.Count);

        var tasks = events.Select(domainEvent => PublishAsync(domainEvent, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogDebug("Successfully published {EventCount} domain events", events.Count);
    }
}