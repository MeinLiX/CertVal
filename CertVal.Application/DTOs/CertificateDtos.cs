using CertVal.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record CertificateDto
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string Thumbprint { get; init; } = string.Empty;
    public DateTime NotBefore { get; init; }
    public DateTime NotAfter { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string FileFormat { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public bool IsBundle { get; init; }
    public Guid? ParentCertificateId { get; init; }
    public bool IsSkipped { get; init; }
    public Guid? PreviousCertificateId { get; init; }
    public Guid? NextCertificateId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
    public int DaysUntilExpiry { get; init; }
    public List<CertificateDto> ChildCertificates { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UploadCertificateRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;

    public string? Description { get; init; }
}

public record UploadMultipleCertificatesRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required]
    public IEnumerable<IFormFile> Files { get; init; } = null!;

    public string? Description { get; init; }
}

public record CertificateFilterRequest
{
    public Guid? WorkspaceId { get; init; }
    public string? Subject { get; init; }
    public string? Issuer { get; init; }
    public DateTime? ExpiringBefore { get; init; }
    public DateTime? ExpiringAfter { get; init; }

    public CertificateStatusFilter StatusFilter { get; init; } = CertificateStatusFilter.All;

    public bool? IsBundle { get; init; }
    public string? Status { get; init; }

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    public string? SortBy { get; init; } = "NotAfter";
    public bool SortDescending { get; init; } = false;
}

public record ToggleCertificateSkipRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required]
    public bool IsSkipped { get; init; }
}