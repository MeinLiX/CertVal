using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Certificates")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CertificateDto>>> GetCertificates([FromQuery] CertificateFilterRequest request)
    {
        var result = await _certificateService.GetCertificatesAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CertificateDto>> GetCertificate(Guid id)
    {
        var result = await _certificateService.GetCertificateByIdAsync(id);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new { message = result.Error });
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CertificateDto>> UploadCertificate([FromForm] UploadCertificateRequest request)
    {
        var result = await _certificateService.UploadCertificateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetCertificate), new { id = result.Value.Id }, result.Value);
    }

    [HttpPost("upload/multiple")]
    [ProducesResponseType(typeof(BulkUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkUploadResultDto>> UploadMultipleCertificates([FromForm] UploadMultipleCertificatesRequest request)
    {
        var result = await _certificateService.UploadMultipleCertificatesAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCertificate(Guid id)
    {
        var result = await _certificateService.DeleteCertificateAsync(id);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
                return NotFound(new { message = result.Error });
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IEnumerable<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetExpiringCertificates([FromQuery] int daysAhead = 30)
    {
        var result = await _certificateService.GetExpiringCertificatesAsync(daysAhead);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }
}