using CertVal.Application.DTOs;
using CertVal.Application.Features.Exports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Exports")]
public class ExportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("certificates/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCertificatesAsCsv(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool includeExpired = true,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportCertificatesAsCsvQuery
        {
            WorkspaceId = workspaceId,
            IncludeExpired = includeExpired
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        var (fileContents, fileName, contentType) = result.Value;
        return File(fileContents, contentType, fileName);
    }

    [HttpGet("certificates/json")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCertificatesAsJson(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool includeExpired = true,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportCertificatesAsJsonQuery
        {
            WorkspaceId = workspaceId,
            IncludeExpired = includeExpired
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        var (fileContents, fileName, contentType) = result.Value;
        return File(fileContents, contentType, fileName);
    }

    [HttpGet("workspace/{workspaceId:guid}/report")]
    [ProducesResponseType(typeof(WorkspaceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceReportDto>> GenerateWorkspaceReport(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new GenerateWorkspaceReportQuery(workspaceId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }
}