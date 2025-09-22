using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Dashboard.Queries;

public record GetExpiringCertificatesForDashboardQuery : IRequest<Result<IEnumerable<CertificateDto>>>
{
    public int DaysAhead { get; init; } = 30;
}

public class GetExpiringCertificatesForDashboardQueryValidator : AbstractValidator<GetExpiringCertificatesForDashboardQuery>
{
    public GetExpiringCertificatesForDashboardQueryValidator()
    {
        RuleFor(x => x.DaysAhead)
            .GreaterThan(0).WithMessage("Days ahead must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Days ahead must not exceed 365");
    }
}

public class GetExpiringCertificatesForDashboardQueryHandler : IRequestHandler<GetExpiringCertificatesForDashboardQuery, Result<IEnumerable<CertificateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetExpiringCertificatesForDashboardQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<CertificateDto>>> Handle(GetExpiringCertificatesForDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<CertificateDto>>("User not authenticated");

        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
        var workspaceIds = workspaces.Select(w => w.Id).ToList();

        var expiringCertificates = new List<Core.Entities.Certificate>();
        var cutoffDate = DateTime.UtcNow.AddDays(request.DaysAhead);

        foreach (var workspaceId in workspaceIds)
        {
            var workspaceCerts = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId, cancellationToken);
            var expiring = workspaceCerts.Where(c => c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow);
            expiringCertificates.AddRange(expiring);
        }

        var certificateDtos = expiringCertificates
            .OrderBy(c => c.NotAfter)
            .Select(c => MapToCertificateDto(c))
            .ToList();

        return Result.Success<IEnumerable<CertificateDto>>(certificateDtos);
    }

    private CertificateDto MapToCertificateDto(Core.Entities.Certificate certificate)
    {
        var dto = certificate.Adapt<CertificateDto>();
        return dto with
        {
            DaysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).Days,
            Status = certificate.Status.ToString(),
            FileFormat = certificate.FileFormat.ToString(),
            ChildCertificates = certificate.ChildCertificates.Select(MapToCertificateDto).ToList()
        };
    }
}