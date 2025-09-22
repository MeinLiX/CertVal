using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Dashboard.Queries;

public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;

public class GetDashboardStatsQueryValidator : AbstractValidator<GetDashboardStatsQuery>
{
    public GetDashboardStatsQueryValidator()
    {
    }
}

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardStatsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<DashboardStatsDto>("User not authenticated");

        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
        var workspaceIds = workspaces.Select(w => w.Id).ToList();

        var allCertificates = new List<Core.Entities.Certificate>();
        foreach (var workspaceId in workspaceIds)
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId, cancellationToken);
            allCertificates.AddRange(certificates);
        }

        var now = DateTime.UtcNow;
        var totalWorkspaces = workspaces.Count();
        var totalCertificates = allCertificates.Count;
        var expiredCertificates = allCertificates.Count(c => c.NotAfter <= now);
        var expiringIn7Days = allCertificates.Count(c => c.NotAfter > now && c.NotAfter <= now.AddDays(7));
        var expiringIn30Days = allCertificates.Count(c => c.NotAfter > now && c.NotAfter <= now.AddDays(30));
        var validCertificates = allCertificates.Count(c => c.NotAfter > now.AddDays(30));

        var stats = new DashboardStatsDto
        {
            TotalWorkspaces = totalWorkspaces,
            TotalCertificates = totalCertificates,
            ExpiredCertificates = expiredCertificates,
            ExpiringIn7Days = expiringIn7Days,
            ExpiringIn30Days = expiringIn30Days,
            ValidCertificates = validCertificates
        };

        return Result.Success(stats);
    }
}