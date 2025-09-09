using CertVal.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record ApiTokenDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TokenPrefix { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateApiTokenRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public ApiTokenScope Scope { get; init; } = ApiTokenScope.ReadOnly;

    public DateTime? ExpiresAt { get; init; }
}

public record CreateApiTokenResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty; // Full token only shown on creation
    public string TokenPrefix { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}