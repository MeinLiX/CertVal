using CertVal.Application.DTOs;
using CertVal.Application.Features.Endpoints.Commands;
using CertVal.Application.Features.Endpoints.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/endpoints")]
[Authorize]
[Tags("Monitored Endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EndpointsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MonitoredEndpointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MonitoredEndpointDto>>> GetEndpoints(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkspaceEndpointsQuery(workspaceId), cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Contains("Access denied") ? Forbid() : BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MonitoredEndpointDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MonitoredEndpointDto>> CreateEndpoint(
        Guid workspaceId,
        [FromBody] CreateMonitoredEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateMonitoredEndpointCommand(workspaceId, request.Host, request.Port, request.CheckIntervalMinutes);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return result.Error.Contains("Access denied") ? Forbid() : BadRequest(new ErrorResponseDto(result.Error));

        return CreatedAtAction(nameof(GetEndpoints), new { workspaceId }, result.Value);
    }

    [HttpPut("{endpointId:guid}")]
    [ProducesResponseType(typeof(MonitoredEndpointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MonitoredEndpointDto>> UpdateEndpoint(
        Guid workspaceId,
        Guid endpointId,
        [FromBody] UpdateMonitoredEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMonitoredEndpointCommand(
            workspaceId, endpointId, request.Host, request.Port, request.IsEnabled, request.CheckIntervalMinutes);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found")) return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied")) return Forbid();
            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }

    [HttpDelete("{endpointId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEndpoint(
        Guid workspaceId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteMonitoredEndpointCommand(workspaceId, endpointId), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found")) return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied")) return Forbid();
            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return NoContent();
    }
}
