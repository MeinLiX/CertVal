using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/[controller]")]
[Authorize]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(IEnumerable<NotificationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<NotificationRuleDto>>> GetNotificationRules(Guid workspaceId)
    {
        if (!await CanAccessWorkspace(workspaceId))
            return Forbid();

        var rules = await _unitOfWork.NotificationRules.GetByWorkspaceAsync(workspaceId);
        var ruleDtos = rules.Select(r => new NotificationRuleDto
        {
            Id = r.Id,
            WorkspaceId = r.WorkspaceId,
            Name = r.Name,
            IsEnabled = r.IsEnabled,
            DaysBeforeExpiry = r.DaysBeforeExpiry,
            Frequency = r.Frequency.ToString(),
            ChannelType = r.ChannelType.ToString(),
            ChannelConfig = r.ChannelConfig,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });

        return Ok(ruleDtos);
    }

    [HttpPost("rules")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> CreateNotificationRule(
        Guid workspaceId,
        CreateNotificationRuleRequest request)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var rule = NotificationRule.Create(
            workspaceId,
            request.Name,
            request.DaysBeforeExpiry,
            request.ChannelType,
            request.ChannelConfig,
            request.Frequency
        );

        await _unitOfWork.NotificationRules.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        var ruleDto = new NotificationRuleDto
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            DaysBeforeExpiry = rule.DaysBeforeExpiry,
            Frequency = rule.Frequency.ToString(),
            ChannelType = rule.ChannelType.ToString(),
            ChannelConfig = rule.ChannelConfig,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return CreatedAtAction(nameof(GetNotificationRules), new { workspaceId }, ruleDto);
    }

    [HttpPut("rules/{ruleId:guid}")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> UpdateNotificationRule(
        Guid workspaceId,
        Guid ruleId,
        UpdateNotificationRuleRequest request)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var rule = await _unitOfWork.NotificationRules.GetByIdAsync(ruleId);
        if (rule == null || rule.WorkspaceId != workspaceId)
            return NotFound();

        rule.UpdateConfig(request.ChannelConfig);

        await _unitOfWork.NotificationRules.UpdateAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        var ruleDto = new NotificationRuleDto
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            DaysBeforeExpiry = rule.DaysBeforeExpiry,
            Frequency = rule.Frequency.ToString(),
            ChannelType = rule.ChannelType.ToString(),
            ChannelConfig = rule.ChannelConfig,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return Ok(ruleDto);
    }

    [HttpPost("rules/{ruleId:guid}/toggle")]
    [ProducesResponseType(typeof(NotificationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationRuleDto>> ToggleNotificationRule(
        Guid workspaceId,
        Guid ruleId)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var rule = await _unitOfWork.NotificationRules.GetByIdAsync(ruleId);
        if (rule == null || rule.WorkspaceId != workspaceId)
            return NotFound();

        rule.Toggle();

        await _unitOfWork.NotificationRules.UpdateAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        var ruleDto = new NotificationRuleDto
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            DaysBeforeExpiry = rule.DaysBeforeExpiry,
            Frequency = rule.Frequency.ToString(),
            ChannelType = rule.ChannelType.ToString(),
            ChannelConfig = rule.ChannelConfig,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return Ok(ruleDto);
    }

    [HttpDelete("rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteNotificationRule(Guid workspaceId, Guid ruleId)
    {
        if (!await CanManageWorkspace(workspaceId))
            return Forbid();

        var rule = await _unitOfWork.NotificationRules.GetByIdAsync(ruleId);
        if (rule == null || rule.WorkspaceId != workspaceId)
            return NotFound();

        await _unitOfWork.NotificationRules.DeleteAsync(ruleId);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<NotificationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<NotificationHistoryDto>>> GetNotificationHistory(
        Guid workspaceId,
        [FromQuery] Guid? certificateId = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        if (!await CanAccessWorkspace(workspaceId))
            return Forbid();

        IEnumerable<NotificationHistory> history;

        if (certificateId.HasValue)
        {
            var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId.Value);
            if (certificate == null || certificate.WorkspaceId != workspaceId)
                return NotFound();

            history = await _unitOfWork.NotificationHistory.GetByCertificateAsync(certificateId.Value);
        }
        else
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId);
            var certificateIds = certificates.Select(c => c.Id).ToList();

            history = new List<NotificationHistory>();
            foreach (var certId in certificateIds)
            {
                var certHistory = await _unitOfWork.NotificationHistory.GetByCertificateAsync(certId);
                history = history.Concat(certHistory);
            }
        }

        var pagedHistory = history
            .OrderByDescending(h => h.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var historyDtos = pagedHistory.Select(h => new NotificationHistoryDto
        {
            Id = h.Id,
            NotificationRuleId = h.NotificationRuleId,
            CertificateId = h.CertificateId,
            Status = h.Status.ToString(),
            ChannelType = h.ChannelType.ToString(),
            Recipient = h.Recipient,
            Subject = h.Subject,
            Message = h.Message,
            ScheduledAt = h.ScheduledAt,
            SentAt = h.SentAt,
            DeliveredAt = h.DeliveredAt,
            ErrorMessage = h.ErrorMessage,
            RetryCount = h.RetryCount,
            CreatedAt = h.CreatedAt
        });

        return Ok(historyDtos);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value);
        return membership?.CanManageWorkspace ?? false;
    }
}