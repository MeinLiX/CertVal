using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Tags("Exports & Reports")]
public class ExportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ExportsController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet("certificates/csv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCertificatesAsCsv(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool includeExpired = true)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userWorkspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);
        var accessibleWorkspaceIds = userWorkspaces.Select(w => w.Id).ToHashSet();

        if (workspaceId.HasValue)
        {
            if (!accessibleWorkspaceIds.Contains(workspaceId.Value))
                return Forbid();
            accessibleWorkspaceIds = new HashSet<Guid> { workspaceId.Value };
        }

        var allCertificates = new List<Core.Entities.Certificate>();
        foreach (var wsId in accessibleWorkspaceIds)
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(wsId);
            allCertificates.AddRange(certificates);
        }

        if (!includeExpired)
        {
            allCertificates = allCertificates.Where(c => c.NotAfter > DateTime.UtcNow).ToList();
        }

        var csv = GenerateCertificatesCsv(allCertificates);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"certificates_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    [HttpGet("certificates/json")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCertificatesAsJson(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool includeExpired = true)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userWorkspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);
        var accessibleWorkspaceIds = userWorkspaces.Select(w => w.Id).ToHashSet();

        if (workspaceId.HasValue)
        {
            if (!accessibleWorkspaceIds.Contains(workspaceId.Value))
                return Forbid();
            accessibleWorkspaceIds = new HashSet<Guid> { workspaceId.Value };
        }

        var allCertificates = new List<Core.Entities.Certificate>();
        var workspaceNames = new Dictionary<Guid, string>();

        foreach (var wsId in accessibleWorkspaceIds)
        {
            var workspace = userWorkspaces.First(w => w.Id == wsId);
            workspaceNames[wsId] = workspace.Name;

            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(wsId);
            allCertificates.AddRange(certificates);
        }

        if (!includeExpired)
        {
            allCertificates = allCertificates.Where(c => c.NotAfter > DateTime.UtcNow).ToList();
        }

        var exportData = new
        {
            ExportedAt = DateTime.UtcNow,
            ExportedBy = _currentUser.Email,
            TotalCertificates = allCertificates.Count,
            IncludeExpired = includeExpired,
            Workspaces = workspaceNames,
            Certificates = allCertificates.Select(c => new
            {
                c.Id,
                c.WorkspaceId,
                WorkspaceName = workspaceNames[c.WorkspaceId],
                c.Subject,
                c.Issuer,
                c.SerialNumber,
                c.Thumbprint,
                c.NotBefore,
                c.NotAfter,
                c.OriginalFileName,
                FileFormat = c.FileFormat.ToString(),
                c.FileSize,
                c.IsBundle,
                c.ParentCertificateId,
                Status = c.Status.ToString(),
                IsExpired = c.IsExpired,
                DaysUntilExpiry = (c.NotAfter - DateTime.UtcNow).Days,
                c.CreatedAt,
                c.UpdatedAt
            }).OrderBy(c => c.NotAfter)
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"certificates_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpGet("workspace/{workspaceId:guid}/report")]
    [ProducesResponseType(typeof(WorkspaceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceReportDto>> GenerateWorkspaceReport(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        if (!await CanAccessWorkspace(workspaceId))
            return Forbid();

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId);
        var notificationRules = await _unitOfWork.NotificationRules.GetByWorkspaceAsync(workspaceId);

        var now = DateTime.UtcNow;
        var certificatesList = certificates.ToList();

        var recentNotifications = new List<Core.Entities.NotificationHistory>();
        foreach (var cert in certificatesList.Take(10))
        {
            var history = await _unitOfWork.NotificationHistory.GetByCertificateAsync(cert.Id);
            recentNotifications.AddRange(history.Where(h => h.CreatedAt >= now.AddDays(-30)));
        }

        var report = new WorkspaceReportDto
        {
            WorkspaceId = workspaceId,
            WorkspaceName = workspace!.Name,
            GeneratedAt = now,
            GeneratedBy = _currentUser.Email!,

            CertificateStats = new CertificateStatsDto
            {
                Total = certificatesList.Count,
                Valid = certificatesList.Count(c => c.NotAfter > now.AddDays(30)),
                ExpiringIn30Days = certificatesList.Count(c => c.NotAfter <= now.AddDays(30) && c.NotAfter > now),
                ExpiringIn7Days = certificatesList.Count(c => c.NotAfter <= now.AddDays(7) && c.NotAfter > now),
                Expired = certificatesList.Count(c => c.NotAfter <= now),
                Bundles = certificatesList.Count(c => c.IsBundle),
                FormatBreakdown = certificatesList
                    .GroupBy(c => c.FileFormat.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            },

            NotificationStats = new ReportNotificationStatsDto
            {
                ActiveRules = notificationRules.Count(r => r.IsEnabled),
                TotalRules = notificationRules.Count(),
                RecentNotifications = recentNotifications.Count,
                SuccessfulNotifications = recentNotifications.Count(n =>
                    n.Status == Core.Enums.NotificationStatus.Sent ||
                    n.Status == Core.Enums.NotificationStatus.Delivered),
                FailedNotifications = recentNotifications.Count(n => n.Status == Core.Enums.NotificationStatus.Failed)
            },

            CriticalCertificates = certificatesList
                .Where(c => c.NotAfter <= now.AddDays(30) && c.NotAfter > now)
                .OrderBy(c => c.NotAfter)
                .Take(10)
                .Select(c => new CertificateSummaryDto
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    NotAfter = c.NotAfter,
                    DaysUntilExpiry = (c.NotAfter - now).Days,
                    OriginalFileName = c.OriginalFileName
                }).ToList()
        };

        return Ok(report);
    }

    private static string GenerateCertificatesCsv(List<Core.Entities.Certificate> certificates)
    {
        var csv = new StringBuilder();

        csv.AppendLine("Subject,Issuer,Serial Number,Thumbprint,Not Before,Not After,Days Until Expiry,Original File Name,File Format,File Size,Is Bundle,Status,Is Expired,Created At");

        foreach (var cert in certificates.OrderBy(c => c.NotAfter))
        {
            var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).Days;

            csv.AppendLine($"\"{EscapeCsv(cert.Subject)}\"," +
                          $"\"{EscapeCsv(cert.Issuer)}\"," +
                          $"\"{cert.SerialNumber ?? ""}\"," +
                          $"\"{cert.Thumbprint}\"," +
                          $"{cert.NotBefore:yyyy-MM-dd HH:mm:ss}," +
                          $"{cert.NotAfter:yyyy-MM-dd HH:mm:ss}," +
                          $"{daysUntilExpiry}," +
                          $"\"{EscapeCsv(cert.OriginalFileName)}\"," +
                          $"{cert.FileFormat}," +
                          $"{cert.FileSize}," +
                          $"{cert.IsBundle}," +
                          $"{cert.Status}," +
                          $"{cert.IsExpired}," +
                          $"{cert.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains('"'))
            field = field.Replace("\"", "\"\"");
        return field;
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value);
    }
}