using CertVal.Application.DTOs;
using CertVal.Application.Features.ApiTokens.Commands;
using CertVal.Application.Features.ApiTokens.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("API Tokens")]
public class ApiTokensController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApiTokensController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ApiTokenDto>>> GetTokens(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetApiTokensQuery { IncludeInactive = includeInactive }, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateApiTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateApiTokenResponse>> CreateToken(
        [FromBody] CreateApiTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return CreatedAtAction(nameof(GetTokens), result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RevokeApiTokenCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return NoContent();
    }
}