using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/invitations")]
[Tags("Invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public InvitationsController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(InvitationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitationDetails(string token)
    {
        var membership = await _unitOfWork.WorkspaceMembers.GetByInvitationTokenAsync(token);

        if (membership == null || (membership.InvitationTokenExpiresAt.HasValue && membership.InvitationTokenExpiresAt < DateTime.UtcNow))
        {
            return NotFound(new ErrorResponseDto("Invitation is invalid or has expired."));
        }

        return Ok(new InvitationDetailsDto
        {
            WorkspaceId = membership.Workspace.Id,
            WorkspaceName = membership.Workspace.Name,
            InvitedUserEmail = membership.User.Email
        });
    }

    [Authorize]
    [HttpPost("{token}/accept")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        var membership = await _unitOfWork.WorkspaceMembers.GetByInvitationTokenAsync(token);

        if (membership == null || (membership.InvitationTokenExpiresAt.HasValue && membership.InvitationTokenExpiresAt < DateTime.UtcNow))
        {
            return NotFound(new ErrorResponseDto("Invitation is invalid or has expired."));
        }

        if (_currentUser.UserId.HasValue && membership.UserId != _currentUser.UserId)
        {
            return Forbid();
        }

        membership.AcceptInvitation();
        await _unitOfWork.WorkspaceMembers.UpdateAsync(membership);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new MessageResponseDto("Invitation accepted successfully."));
    }
}