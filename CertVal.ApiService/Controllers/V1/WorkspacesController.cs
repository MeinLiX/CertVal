using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspacesController(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<WorkspaceDto>>> GetWorkspaces(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _workspaceService.GetUserWorkspacesAsync(pageNumber, pageSize);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceDto>> GetWorkspace(Guid id)
    {
        var result = await _workspaceService.GetWorkspaceByIdAsync(id);

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

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace(CreateWorkspaceRequest request)
    {
        var result = await _workspaceService.CreateWorkspaceAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(result.Error));

        return CreatedAtAction(nameof(GetWorkspace), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceDto>> UpdateWorkspace(Guid id, UpdateWorkspaceRequest request)
    {
        var result = await _workspaceService.UpdateWorkspaceAsync(id, request);

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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWorkspace(Guid id)
    {
        var result = await _workspaceService.DeleteWorkspaceAsync(id);

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
}