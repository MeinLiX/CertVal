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

            using var scope = _serviceProvider.CreateScope();

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var result = handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
                            if (result is Task taskResult)
                            {
                                await taskResult;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in domain event handler {HandlerType} for event {EventType}",
                                handler?.GetType().Name, domainEvent.GetType().Name);
                        }
                    }, cancellationToken);

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

        foreach (var domainEvent in events)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }

        _logger.LogDebug("Successfully published {EventCount} domain events", events.Count);
    }
}