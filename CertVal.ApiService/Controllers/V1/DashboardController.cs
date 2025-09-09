using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        // Get user's workspaces
        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);
        var workspaceIds = workspaces.Select(w => w.Id).ToList();

        // Get all certificates from user's workspaces
        var allCertificates = new List<Core.Entities.Certificate>();
        foreach (var workspaceId in workspaceIds)
        {
            var workspaceCerts = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId);
            allCertificates.AddRange(workspaceCerts);
        }

        var now = DateTime.UtcNow;

        var stats = new DashboardStatsDto
        {
            TotalWorkspaces = workspaces.Count(),
            TotalCertificates = allCertificates.Count,
            ExpiredCertificates = allCertificates.Count(c => c.NotAfter <= now),
            ExpiringIn7Days = allCertificates.Count(c => c.NotAfter > now && c.NotAfter <= now.AddDays(7)),
            ExpiringIn30Days = allCertificates.Count(c => c.NotAfter > now && c.NotAfter <= now.AddDays(30)),
            ValidCertificates = allCertificates.Count(c => c.NotAfter > now.AddDays(30))
        };

        return Ok(stats);
    }

    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(IEnumerable<RecentActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivity()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        // Get recent events from event store
        var recentEvents = await _unitOfWork.EventStore.GetEventsByUserAsync(
            _currentUser.UserId.Value.ToString(),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        var activities = recentEvents
            .OrderByDescending(e => e.OccurredAt)
            .Take(20)
            .Select(e => new RecentActivityDto
            {
                Id = e.Id,
                EventType = e.EventType,
                Description = GetEventDescription(e.EventType, e.EventData),
                OccurredAt = e.OccurredAt,
                AggregateId = e.AggregateId
            });

        return Ok(activities);
    }

    [HttpGet("expiring-certificates")]
    [ProducesResponseType(typeof(IEnumerable<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetExpiringCertificates(
        [FromQuery] int daysAhead = 30)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        // Get user's workspaces
        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);
        var workspaceIds = workspaces.Select(w => w.Id).ToList();

        // Get expiring certificates from all accessible workspaces
        var expiringCertificates = new List<Core.Entities.Certificate>();
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        foreach (var workspaceId in workspaceIds)
        {
            var workspaceCerts = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId);
            var expiring = workspaceCerts.Where(c => c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow);
            expiringCertificates.AddRange(expiring);
        }

        var certificateDtos = expiringCertificates
            .OrderBy(c => c.NotAfter)
            .Select(c => new CertificateDto
            {
                Id = c.Id,
                WorkspaceId = c.WorkspaceId,
                Subject = c.Subject,
                Issuer = c.Issuer,
                SerialNumber = c.SerialNumber,
                Thumbprint = c.Thumbprint,
                NotBefore = c.NotBefore,
                NotAfter = c.NotAfter,
                OriginalFileName = c.OriginalFileName,
                FileFormat = c.FileFormat.ToString(),
                FileSize = c.FileSize,
                IsBundle = c.IsBundle,
                ParentCertificateId = c.ParentCertificateId,
                Status = c.Status.ToString(),
                IsExpired = c.IsExpired,
                DaysUntilExpiry = (c.NotAfter - DateTime.UtcNow).Days,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

        return Ok(certificateDtos);
    }

    private static string GetEventDescription(string eventType, string eventData)
    {
        return eventType switch
        {
            "CertificateUploadedEvent" => "Certificate uploaded",
            "CertificateExpiringEvent" => "Certificate expiring soon",
            "CertificateExpiredEvent" => "Certificate expired",
            "WorkspaceCreatedEvent" => "Workspace created",
            "WorkspaceUpdatedEvent" => "Workspace updated",
            "NotificationSentEvent" => "Notification sent",
            "NotificationFailedEvent" => "Notification failed",
            "ApiTokenCreatedEvent" => "API token created",
            "ApiTokenUsedEvent" => "API token used",
            _ => eventType.Replace("Event", "")
        };
    }
}