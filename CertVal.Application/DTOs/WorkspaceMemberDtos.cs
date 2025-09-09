using CertVal.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record WorkspaceMemberDto
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
    public UserDto User { get; init; } = null!;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? InvitedAt { get; init; }
    public DateTime? JoinedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record InviteMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public WorkspaceRole Role { get; init; } = WorkspaceRole.Viewer;
}

public record UpdateMemberRoleRequest
{
    [Required]
    public WorkspaceRole Role { get; init; }
}