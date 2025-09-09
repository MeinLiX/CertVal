namespace CertVal.Application.DTOs;

public record DashboardStatsDto
{
    public int TotalWorkspaces { get; init; }
    public int TotalCertificates { get; init; }
    public int ExpiredCertificates { get; init; }
    public int ExpiringIn7Days { get; init; }
    public int ExpiringIn30Days { get; init; }
    public int ValidCertificates { get; init; }
}

public record RecentActivityDto
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public Guid? AggregateId { get; init; }
}