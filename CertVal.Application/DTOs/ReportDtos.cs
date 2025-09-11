namespace CertVal.Application.DTOs;

public record WorkspaceReportDto
{
    public Guid WorkspaceId { get; init; }
    public string WorkspaceName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public CertificateStatsDto CertificateStats { get; init; } = null!;
    public ReportNotificationStatsDto NotificationStats { get; init; } = null!;
    public List<CertificateSummaryDto> CriticalCertificates { get; init; } = [];
}

public record CertificateStatsDto
{
    public int Total { get; init; }
    public int Valid { get; init; }
    public int ExpiringIn30Days { get; init; }
    public int ExpiringIn7Days { get; init; }
    public int Expired { get; init; }
    public int Bundles { get; init; }
    public Dictionary<string, int> FormatBreakdown { get; init; } = new();
}

public record ReportNotificationStatsDto
{
    public int ActiveRules { get; init; }
    public int TotalRules { get; init; }
    public int RecentNotifications { get; init; }
    public int SuccessfulNotifications { get; init; }
    public int FailedNotifications { get; init; }
}

public record CertificateSummaryDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public DateTime NotAfter { get; init; }
    public int DaysUntilExpiry { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
}
