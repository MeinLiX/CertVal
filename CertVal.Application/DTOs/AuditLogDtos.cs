namespace CertVal.Application.DTOs;

public record AuditLogEntryDto
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? AggregateId { get; init; }
    public DateTime OccurredAt { get; init; }
}
