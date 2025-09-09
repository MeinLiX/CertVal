using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record WorkspaceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public UserDto Owner { get; init; } = null!;
    public int MaxCertificates { get; init; }
    public bool IsPublic { get; init; }
    public bool AllowMemberInvites { get; init; }
    public int CertificateCount { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateWorkspaceRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 10000)]
    public int MaxCertificates { get; init; } = 1000;

    public bool IsPublic { get; init; } = false;
    public bool AllowMemberInvites { get; init; } = true;
}

public record UpdateWorkspaceRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 10000)]
    public int MaxCertificates { get; init; }

    public bool IsPublic { get; init; }
    public bool AllowMemberInvites { get; init; }
}