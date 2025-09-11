using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IEventStoreRepository
{
    Task<StoredEvent> SaveEventAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default);
    Task SaveEventsAsync(IEnumerable<StoredEvent> storedEvents, CancellationToken cancellationToken = default);

    Task<StoredEvent?> GetEventByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<StoredEvent?> GetEventByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetEventsByAggregateAsync(
        string aggregateType,
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetEventsByTypeAsync(
        string eventType,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetEventsByUserAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetEventsByCorrelationAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetEventsAfterAsync(
        long afterEventId,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StoredEvent>> GetRecentEventsAsync(
        int take = 100,
        CancellationToken cancellationToken = default);

    Task<long> GetEventCountAsync(CancellationToken cancellationToken = default);
    Task<long> GetEventCountByTypeAsync(string eventType, CancellationToken cancellationToken = default);
}