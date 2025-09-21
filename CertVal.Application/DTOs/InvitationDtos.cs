namespace CertVal.Application.DTOs;

public record InvitationDetailsDto
{
    public Guid WorkspaceId { get; init; }
    public string WorkspaceName { get; init; } = string.Empty;
    public string InvitedUserEmail { get; init; } = string.Empty;
}