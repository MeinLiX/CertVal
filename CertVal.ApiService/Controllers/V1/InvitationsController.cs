using CertVal.Application.DTOs;
using CertVal.Application.Features.Invitations.Commands;
using CertVal.Application.Features.Invitations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/invitations")]
[Tags("Invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(InvitationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitationDetails(string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvitationDetailsQuery { Token = token }, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{token}/accept")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptInvitation(string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptInvitationCommand { Token = token }, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found") || result.Error.Contains("expired"))
                return NotFound(new ErrorResponseDto(result.Error));

            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(new MessageResponseDto("Invitation accepted successfully."));
    }
}