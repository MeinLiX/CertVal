using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Application.Features.Tools.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Tools")]
public class ToolsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRateLimitService _rateLimit;

    public ToolsController(IMediator mediator, IRateLimitService rateLimit)
    {
        _mediator = mediator;
        _rateLimit = rateLimit;
    }

    /// <summary>
    /// Public, no-auth SSL/TLS checker. Connects to the given host and reports the
    /// served certificate chain, expiry, hostname match and negotiated protocol.
    /// </summary>
    [HttpPost("ssl-check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SslCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SslCheckResultDto>> CheckSsl(
        [FromBody] SslCheckRequest request,
        CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!await _rateLimit.IsAllowedAsync($"ssl_check:{clientIp}", TimeSpan.FromSeconds(3)))
            return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponseDto("Too many requests. Please wait a moment and try again."));

        var result = await _mediator.Send(new CheckSslQuery(request.Host, request.Port), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }
}
