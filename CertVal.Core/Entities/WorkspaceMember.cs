using CertVal.Core.Enums;
using CertVal.Core.Events;
using CertVal.Core.Utils;

namespace CertVal.Core.Entities;

public class WorkspaceMember : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; } = WorkspaceRole.Viewer;
    public WorkspaceMemberStatus Status { get; private set; } = WorkspaceMemberStatus.Active;

    public Guid? InvitedByUserId { get; private set; }
    public DateTime? InvitedAt { get; private set; }
    public DateTime? JoinedAt { get; private set; }

    public string? InvitationToken { get; private set; }
    public DateTime? InvitationTokenExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Workspace Workspace { get; private set; } = null!;
    public virtual User User { get; private set; } = null!;
    public virtual User? InvitedByUser { get; private set; }

    private WorkspaceMember() { } // EF Constructor

    public static WorkspaceMember Create(
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        Guid? invitedByUserId = null)
    {
        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            InvitedByUserId = invitedByUserId,
            InvitedAt = invitedByUserId.HasValue ? DateTime.UtcNow : null,
            JoinedAt = invitedByUserId.HasValue ? null : DateTime.UtcNow
        };

        if (invitedByUserId.HasValue)
        {
            member.Status = WorkspaceMemberStatus.Invited;
            member.InvitationToken = TokenGenerator.GenerateUrlSafeToken();
            member.InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            member.AddDomainEvent(new WorkspaceMemberInvitedEvent(workspaceId, userId, invitedByUserId.Value, member.InvitationToken));
        }
        else
        {
            member.AddDomainEvent(new WorkspaceMemberJoinedEvent(workspaceId, userId));
        }

        return member;
    }

    public void AcceptInvitation()
    {
        if (Status != WorkspaceMemberStatus.Invited)
            throw new InvalidOperationException("Only invited members can accept invitations");

        Status = WorkspaceMemberStatus.Active;
        JoinedAt = DateTime.UtcNow;
        InvitationToken = null;
        InvitationTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WorkspaceMemberJoinedEvent(WorkspaceId, UserId));
    }

    public void ChangeRole(WorkspaceRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = WorkspaceMemberStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WorkspaceMemberRemovedEvent(WorkspaceId, UserId));
    }

    public bool CanManageWorkspace => Role == WorkspaceRole.Admin;
    public bool CanManageCertificates => Role is WorkspaceRole.Admin or WorkspaceRole.Editor;
    public bool CanViewCertificates => Role is WorkspaceRole.Admin or WorkspaceRole.Editor or WorkspaceRole.Viewer;
}