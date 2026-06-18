using CertVal.Core.Entities;
using CertVal.Core.Events;
using CertVal.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CertVal.Infrastructure.Events;

public interface IDomainEventDispatcher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

public interface IBackgroundDomainEventDispatcher
{
    void EnqueueEvent(DomainEvent domainEvent);
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

public class BackgroundDomainEventDispatcher : BackgroundService, IBackgroundDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundDomainEventDispatcher> _logger;
    private readonly ConcurrentQueue<DomainEvent> _eventQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public BackgroundDomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<BackgroundDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void EnqueueEvent(DomainEvent domainEvent)
    {
        _eventQueue.Enqueue(domainEvent);
        _semaphore.Release();
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Publishing domain event: {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            using var scope = _serviceProvider.CreateScope();

            await PersistEventAsync(scope.ServiceProvider, domainEvent, cancellationToken);

            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                try
                {
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        var result = handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
                        if (result is Task taskResult)
                        {
                            await taskResult;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in domain event handler {HandlerType} for event {EventType}",
                        handler?.GetType().Name, domainEvent.GetType().Name);
                }
            }

            _logger.LogDebug("Successfully published domain event: {EventType} with {HandlerCount} handlers",
                domainEvent.GetType().Name, handlers.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event: {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.Id);
        }
    }

    private async Task PersistEventAsync(IServiceProvider scopedProvider, DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            var eventStore = scopedProvider.GetRequiredService<IEventStoreRepository>();
            var storedEvent = StoredEvent.FromRuntimeEvent(domainEvent);
            await eventStore.SaveEventAsync(storedEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Persisting the audit record must never break event handling.
            _logger.LogError(ex, "Failed to persist domain event {EventType} with ID {EventId} to the event store",
                domainEvent.GetType().Name, domainEvent.Id);
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        if (!events.Any()) return;

        _logger.LogDebug("Publishing {EventCount} domain events", events.Count);

        foreach (var domainEvent in events)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }

        _logger.LogDebug("Successfully published {EventCount} domain events", events.Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Domain Event Dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _semaphore.WaitAsync(stoppingToken);

                while (_eventQueue.TryDequeue(out var domainEvent))
                {
                    await PublishAsync(domainEvent, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background domain event processing");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Background Domain Event Dispatcher stopped");
    }
}

public class HybridDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IBackgroundDomainEventDispatcher _backgroundDispatcher;

    private static readonly HashSet<Type> ImmediateEvents =
    [
        typeof(UserRegisteredEvent),
        typeof(WorkspaceMemberInvitedEvent)
    ];

    public HybridDomainEventDispatcher(
        IBackgroundDomainEventDispatcher backgroundDispatcher,
        ILogger<HybridDomainEventDispatcher> logger)
    {
        _backgroundDispatcher = backgroundDispatcher;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (ImmediateEvents.Contains(domainEvent.GetType()))
        {
            await _backgroundDispatcher.PublishAsync(domainEvent, cancellationToken);
        }
        else
        {
            _backgroundDispatcher.EnqueueEvent(domainEvent);
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        var immediateEvents = events.Where(e => ImmediateEvents.Contains(e.GetType())).ToList();
        var backgroundEvents = events.Except(immediateEvents).ToList();

        if (immediateEvents.Any())
        {
            await _backgroundDispatcher.PublishAsync(immediateEvents, cancellationToken);
        }

        foreach (var bgEvent in backgroundEvents)
        {
            _backgroundDispatcher.EnqueueEvent(bgEvent);
        }
    }
}