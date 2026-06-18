namespace CertVal.Application.DTOs;

public enum BulkCertificateOperationType
{
    Delete = 1,
    Skip = 2,
    Unskip = 3,
    AddTags = 4,
    RemoveTags = 5
}

public record BulkCertificateOperationRequest
{
    public Guid WorkspaceId { get; init; }
    public List<Guid> CertificateIds { get; init; } = [];
    public BulkCertificateOperationType Operation { get; init; }
    public List<string>? Tags { get; init; }
}

public record BulkOperationResultDto
{
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkOperationItemResult> Failures { get; init; } = [];
}

public record BulkOperationItemResult
{
    public Guid CertificateId { get; init; }
    public string Error { get; init; } = string.Empty;
}
