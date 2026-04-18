namespace CertVal.Core.Messaging;

public sealed record CertificateExpiryDigestMessage
{
    public required string WorkspaceName { get; init; }
    public required IReadOnlyCollection<string> Recipients { get; init; }
    public required IReadOnlyList<CertificateExpiryDigestItem> Items { get; init; }
    public int TotalCount { get; init; }
    public int RemainingCount { get; init; }
    public int ExpiredCount { get; init; }
    public int ExpiringCount { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record CertificateExpiryDigestItem
{
    public required string Subject { get; init; }
    public required string Issuer { get; init; }
    public string? SerialNumber { get; init; }
    public DateTime ExpiryDate { get; init; }
    public int DaysUntilExpiry { get; init; }
    public bool IsExpired { get; init; }
}
