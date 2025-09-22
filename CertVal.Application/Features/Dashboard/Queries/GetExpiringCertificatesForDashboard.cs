using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using MediatR;

namespace CertVal.Application.Features.Dashboard.Queries;

public record GetExpiringCertificatesForDashboardQuery(int DaysAhead) : IRequest<Result<IEnumerable<CertificateDto>>>;

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

        return Result.Success(certificateDtos);
    }
}