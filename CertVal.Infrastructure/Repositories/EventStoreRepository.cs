using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class EventStoreRepository : IEventStoreRepository
{
    private readonly ApplicationDbContext _context;

    public EventStoreRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StoredEvent> SaveEventAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default)
    {
        var entry = await _context.StoredEvents.AddAsync(storedEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task SaveEventsAsync(IEnumerable<StoredEvent> storedEvents, CancellationToken cancellationToken = default)
    {
        var events = storedEvents.ToList();
        if (!events.Any()) return;

        await _context.StoredEvents.AddRangeAsync(events, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredEvent?> GetEventByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<StoredEvent?> GetEventByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsByAggregateAsync(
        string aggregateType,
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .Where(e => e.AggregateType == aggregateType && e.AggregateId == aggregateId)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsByTypeAsync(
        string eventType,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StoredEvents.Where(e => e.EventType == eventType);

        if (fromDate.HasValue)
            query = query.Where(e => e.OccurredAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.OccurredAt <= toDate.Value);

        return await query
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsByUserAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StoredEvents.Where(e => e.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(e => e.OccurredAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.OccurredAt <= toDate.Value);

        return await query
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsByCorrelationAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsAfterAsync(
        long afterEventId,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.StoredEvents
            .Where(e => e.Id > afterEventId)
            .OrderBy(e => e.Id);

        if (take.HasValue)
        {
            return await baseQuery
                .Take(take.Value)
                .ToListAsync(cancellationToken);
        }

        return await baseQuery.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetRecentEventsAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .OrderByDescending(e => e.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetEventCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents.LongCountAsync(cancellationToken);
    }

    public async Task<long> GetEventCountByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await _context.StoredEvents
            .LongCountAsync(e => e.EventType == eventType, cancellationToken);
    }
}