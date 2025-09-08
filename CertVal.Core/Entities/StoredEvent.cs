using System.Text.Json;

namespace CertVal.Core.Entities;

public class StoredEvent
{
    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventData { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid? AggregateId { get; private set; }
    public string? UserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime StoredAt { get; private set; } = DateTime.UtcNow;
    public string? Metadata { get; private set; }

    private StoredEvent() { } // EF Constructor

    public static StoredEvent FromDomainEvent<T>(
        T domainEvent,
        string? aggregateType = null,
        Guid? aggregateId = null,
        string? userId = null,
        string? correlationId = null,
        object? metadata = null) where T : Events.DomainEvent
    {
        return new StoredEvent
        {
            EventId = domainEvent.Id,
            EventType = typeof(T).Name,
            EventData = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }),
            AggregateType = aggregateType ?? string.Empty,
            AggregateId = aggregateId,
            UserId = userId,
            CorrelationId = correlationId,
            OccurredAt = domainEvent.OccurredAt,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };
    }

    public T? GetEventData<T>() where T : Events.DomainEvent
    {
        try
        {
            return JsonSerializer.Deserialize<T>(EventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }

    public Dictionary<string, object>? GetMetadata()
    {
        if (string.IsNullOrWhiteSpace(Metadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata);
        }
        catch
        {
            return null;
        }
    }
}