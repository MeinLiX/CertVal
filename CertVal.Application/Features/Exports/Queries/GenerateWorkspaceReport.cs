using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using MediatR;

namespace CertVal.Application.Features.Exports.Queries;

public record GenerateWorkspaceReportQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceReportDto>>;

public class GenerateWorkspaceReportQueryHandler : IRequestHandler<GenerateWorkspaceReportQuery, Result<WorkspaceReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GenerateWorkspaceReportQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceReportDto>> Handle(GenerateWorkspaceReportQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceReportDto>("User not authenticated");

        if (!await _unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, _currentUser.UserId.Value, cancellationToken))
            return Result.Failure<WorkspaceReportDto>("Access denied");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        var certificates = (await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken)).ToList();
        var notificationRules = (await _unitOfWork.NotificationRules.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken)).ToList();
        var now = DateTime.UtcNow;

        var recentNotifications = new List<Core.Entities.NotificationHistory>();
        foreach (var cert in certificates.Take(10))
        {
            var history = await _unitOfWork.NotificationHistory.GetByCertificateAsync(cert.Id);
            recentNotifications.AddRange(history.Where(h => h.CreatedAt >= now.AddDays(-30)));
        }

        var report = new WorkspaceReportDto
        {
            WorkspaceId = request.WorkspaceId,
            WorkspaceName = workspace!.Name,
            GeneratedAt = now,
            GeneratedBy = _currentUser.Email!,

            CertificateStats = new CertificateStatsDto
            {
                Total = certificates.Count,
                Valid = certificates.Count(c => c.NotAfter > now.AddDays(30)),
                ExpiringIn30Days = certificates.Count(c => c.NotAfter <= now.AddDays(30) && c.NotAfter > now),
                ExpiringIn7Days = certificates.Count(c => c.NotAfter <= now.AddDays(7) && c.NotAfter > now),
                Expired = certificates.Count(c => c.NotAfter <= now),
                Bundles = certificates.Count(c => c.IsBundle),
                FormatBreakdown = certificates
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

            CriticalCertificates = certificates
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

        return Result.Success(report);
    }
}