
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record BulkUploadCertificatesRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required]
    public IFormFileCollection Files { get; init; } = null!;
}

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

public record BulkDeleteCertificatesRequest
{
    [Required]
    public List<Guid> CertificateIds { get; init; } = [];
}

public record BulkDeleteResultDto
{
    public int TotalCertificates { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkDeleteItemResult> Results { get; init; } = [];
}

public record BulkDeleteItemResult
{
    public Guid CertificateId { get; init; }
    public bool Success { get; init; }
    public string? Subject { get; init; }
    public string? ErrorMessage { get; init; }
}