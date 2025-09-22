using CertVal.Application.DTOs;
using CertVal.Application.Features.Api.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiInfoResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiInfoResponseDto>> GetApiInfo(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApiInfoQuery(), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpGet("version")]
    [ProducesResponseType(typeof(VersionResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VersionResponseDto>> GetVersion(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVersionQuery(), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }
}