using CertVal.Application.DTOs;
using CertVal.Application.Features.Notifications.Commands;
using CertVal.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/[controller]")]
[Authorize]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(IEnumerable<NotificationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<NotificationRuleDto>>> GetNotificationRules(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new GetNotificationRulesQuery { WorkspaceId = workspaceId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return Ok(result.Value);
    }

    [HttpPost("rules")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> CreateNotificationRule(
        Guid workspaceId,
        [FromBody] CreateNotificationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateNotificationRuleCommand
        {
            WorkspaceId = workspaceId,
            Name = request.Name,
            DaysBeforeExpiry = request.DaysBeforeExpiry,
            ChannelType = request.ChannelType,
            ChannelConfig = request.ChannelConfig,
            RecipientUserIds = request.RecipientUserIds,
            Frequency = request.Frequency
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("Access denied"))
                return Forbid();

            return BadRequest(new ErrorResponseDto(result.Error));
        }

        return CreatedAtAction(nameof(GetNotificationRules), new { workspaceId }, result.Value);
    }

    [HttpPut("rules/{ruleId:guid}")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> UpdateNotificationRule(
        Guid workspaceId,
        Guid ruleId,
        [FromBody] UpdateNotificationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateNotificationRuleCommand
        {
            WorkspaceId = workspaceId,
            RuleId = ruleId,
            ChannelConfig = request.ChannelConfig
        };

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

    [HttpPost("rules/{ruleId:guid}/toggle")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> ToggleNotificationRule(
        Guid workspaceId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        var command = new ToggleNotificationRuleCommand
        {
            WorkspaceId = workspaceId,
            RuleId = ruleId
        };

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

    [HttpDelete("rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteNotificationRule(
        Guid workspaceId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteNotificationRuleCommand
        {
            WorkspaceId = workspaceId,
            RuleId = ruleId
        };

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

    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<NotificationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<NotificationHistoryDto>>> GetNotificationHistory(
        Guid workspaceId,
        [FromQuery] Guid? certificateId = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationHistoryQuery
        {
            WorkspaceId = workspaceId,
            CertificateId = certificateId,
            PageSize = pageSize,
            PageNumber = pageNumber
        };

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