using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Application.Features.Certificates.Commands;
using CertVal.Application.Features.Certificates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Certificates")]
public class CertificatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CertificatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CertificateDto>>> GetCertificates(
        [FromQuery] GetCertificatesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CertificateDto>> GetCertificate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCertificateByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(BulkUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkUploadResultDto>> UploadCertificates(
        [FromForm] Guid workspaceId,
        [FromForm] IFormFile[] files,
        CancellationToken cancellationToken)
    {
        var command = new UploadCertificatesCommand(workspaceId, files);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCertificate(
        Guid id,
        [FromQuery] bool deleteBundleChildren = true,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteCertificateCommand(id, deleteBundleChildren);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        var query = new DownloadCertificateQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        var (fileContents, fileName, contentType) = result.Value;

        return File(fileContents, contentType, fileName);
    }

    [HttpPatch("{id:guid}/skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ToggleSkip(
        Guid id,
        [FromBody] ToggleCertificateSkipRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ToggleCertificateSkipCommand(request.WorkspaceId, id, request.IsSkipped);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CertificateDto>> UploadUpdatedCertificate(
        Guid id,
        [FromForm] Guid workspaceId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadUpdatedCertificateCommand(workspaceId, id, file);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new ErrorResponseDto(result.Error));
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }
}