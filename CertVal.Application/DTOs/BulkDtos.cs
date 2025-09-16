
namespace CertVal.Application.DTOs;

public record BulkUploadResultDto
{
    public int TotalFiles { get; init; }
    public int SuccessCount { get; init; }
    public int SkippedCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkUploadItemResult> Results { get; init; } = [];

    public string Summary => $"Processed {TotalFiles} files: {SuccessCount} uploaded, {SkippedCount} skipped (duplicates), {FailureCount} failed";
}

public record BulkUploadItemResult
{
    public string FileName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public bool IsSkipped { get; init; }
    public Guid? CertificateId { get; init; }
    public string? Subject { get; init; }
    public string? ErrorMessage { get; init; }
}