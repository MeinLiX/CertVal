using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/members")]
[Authorize]
[Tags("Workspace Members")]
public class WorkspaceMembersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public WorkspaceMembersController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkspaceMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<WorkspaceMemberDto>>> GetMembers(Guid workspaceId)
    {
        if (!await CanAccessWorkspace(workspaceId))
            return Forbid();

        var members = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(workspaceId);
        var memberDtos = members.Select(m => new WorkspaceMemberDto
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            UserId = m.UserId,
            User = new UserDto
            {
                Id = m.User.Id,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                FullName = m.User.FullName,
                IsEmailConfirmed = m.User.IsEmailConfirmed,
                LastLoginAt = m.User.LastLoginAt,
                Status = m.User.Status.ToString(),
                CreatedAt = m.User.CreatedAt
            },
            Role = m.Role.ToString(),
            Status = m.Status.ToString(),
            InvitedAt = m.InvitedAt,
            JoinedAt = m.JoinedAt,
            CreatedAt = m.CreatedAt
        });

        return Ok(memberDtos);
    }

    [HttpPost("invite")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceMemberDto>> InviteMember(
        Guid workspaceId,
        InviteMemberRequest request)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(new ErrorResponseDto("User with this email does not exist"));

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace?.OwnerId == user.Id)
            return BadRequest(new ErrorResponseDto("Workspace owner cannot invite themselves"));

        var existingMember = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, user.Id);
        if (existingMember != null)
            return BadRequest(new ErrorResponseDto("User is already a member of this workspace"));

        var member = WorkspaceMember.Create(
            workspaceId,
            user.Id,
            request.Role,
            _currentUser.UserId!.Value
        );

        await _unitOfWork.WorkspaceMembers.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        var memberDto = new WorkspaceMemberDto
        {
            Id = member.Id,
            WorkspaceId = member.WorkspaceId,
            UserId = member.UserId,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsEmailConfirmed = user.IsEmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            },
            Role = member.Role.ToString(),
            Status = member.Status.ToString(),
            InvitedAt = member.InvitedAt,
            JoinedAt = member.JoinedAt,
            CreatedAt = member.CreatedAt
        };

        return CreatedAtAction(nameof(GetMembers), new { workspaceId }, memberDto);
    }

    [HttpPut("{memberId:guid}/role")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceMemberDto>> UpdateMemberRole(
        Guid workspaceId,
        Guid memberId,
        UpdateMemberRoleRequest request)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var member = await _unitOfWork.WorkspaceMembers.GetByIdAsync(memberId);
        if (member == null || member.WorkspaceId != workspaceId)
            return NotFound();

        member.ChangeRole(request.Role);
        await _unitOfWork.WorkspaceMembers.UpdateAsync(member);
        await _unitOfWork.SaveChangesAsync();

        member = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, member.UserId);

        var memberDto = new WorkspaceMemberDto
        {
            Id = member!.Id,
            WorkspaceId = member.WorkspaceId,
            UserId = member.UserId,
            User = new UserDto
            {
                Id = member.User.Id,
                Email = member.User.Email,
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                FullName = member.User.FullName,
                IsEmailConfirmed = member.User.IsEmailConfirmed,
                LastLoginAt = member.User.LastLoginAt,
                Status = member.User.Status.ToString(),
                CreatedAt = member.User.CreatedAt
            },
            Role = member.Role.ToString(),
            Status = member.Status.ToString(),
            InvitedAt = member.InvitedAt,
            JoinedAt = member.JoinedAt,
            CreatedAt = member.CreatedAt
        };

        return Ok(memberDto);
    }

    [HttpDelete("{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid memberId)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var member = await _unitOfWork.WorkspaceMembers.GetByIdAsync(memberId);
        if (member == null || member.WorkspaceId != workspaceId)
            return NotFound();

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace?.OwnerId == member.UserId && member.UserId == _currentUser.UserId)
        {
            return BadRequest(new ErrorResponseDto("Workspace owner cannot remove themselves. Transfer ownership first."));
        }

        await _unitOfWork.WorkspaceMembers.DeleteAsync(member.Id);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null)
            return NotFound();

        if (workspace.OwnerId == _currentUser.UserId.Value)
            return BadRequest(new ErrorResponseDto("Workspace owner cannot leave. Transfer ownership first."));

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value);
        if (membership == null)
            return NotFound();

        membership.Deactivate();
        await _unitOfWork.WorkspaceMembers.UpdateAsync(membership);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("transfer-ownership")]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkspaceDto>> TransferOwnership(
        Guid workspaceId,
        TransferOwnershipRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null)
            return NotFound();

        if (workspace.OwnerId != _currentUser.UserId.Value)
            return Forbid();

        var newOwner = await _unitOfWork.Users.GetByEmailAsync(request.NewOwnerEmail);
        if (newOwner == null)
            return BadRequest(new ErrorResponseDto("User with this email does not exist"));

        if (!newOwner.IsEmailConfirmed)
            return BadRequest(new ErrorResponseDto("New owner must have confirmed email address"));

        if (newOwner.Id == workspace.OwnerId)
            return BadRequest(new ErrorResponseDto("User is already the workspace owner"));

        var existingMembership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, newOwner.Id);

        //temp :)
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var oldOwnerId = workspace.OwnerId;
            workspace.TransferOwnership(newOwner.Id);
            await _unitOfWork.Workspaces.UpdateAsync(workspace);

            if (existingMembership != null)
            {
                await _unitOfWork.WorkspaceMembers.DeleteAsync(existingMembership.Id);
            }

            var oldOwnerMembership = WorkspaceMember.Create(
                workspaceId,
                oldOwnerId,
                Core.Enums.WorkspaceRole.Admin,
                newOwner.Id
            );
            oldOwnerMembership.AcceptInvitation();

            await _unitOfWork.WorkspaceMembers.AddAsync(oldOwnerMembership);
        });

        workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);

        var workspaceDto = new WorkspaceDto
        {
            Id = workspace!.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OwnerId = workspace.OwnerId,
            Owner = new UserDto
            {
                Id = workspace.Owner.Id,
                Email = workspace.Owner.Email,
                FirstName = workspace.Owner.FirstName,
                LastName = workspace.Owner.LastName,
                FullName = workspace.Owner.FullName,
                IsEmailConfirmed = workspace.Owner.IsEmailConfirmed,
                LastLoginAt = workspace.Owner.LastLoginAt,
                Status = workspace.Owner.Status.ToString(),
                CreatedAt = workspace.Owner.CreatedAt
            },
            MaxCertificates = workspace.MaxCertificates,
            IsPublic = workspace.IsPublic,
            AllowMemberInvites = workspace.AllowMemberInvites,
            CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id),
            MemberCount = workspace.Members.Count + 1,
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt
        };

        return Ok(workspaceDto);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value);
        return membership?.CanManageWorkspace ?? false;
    }
}