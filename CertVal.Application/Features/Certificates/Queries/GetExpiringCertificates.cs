using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Certificates.Queries;

public record GetExpiringCertificatesQuery(int DaysAhead) : IRequest<Result<IEnumerable<CertificateDto>>>;

public class GetExpiringCertificatesQueryHandler : IRequestHandler<GetExpiringCertificatesQuery, Result<IEnumerable<CertificateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetExpiringCertificatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<CertificateDto>>> Handle(GetExpiringCertificatesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<CertificateDto>>("User not authenticated");

        var certificates = await _unitOfWork.Certificates.GetExpiringAsync(request.DaysAhead, cancellationToken);

        var accessibleCertificates = new List<Certificate>();
        foreach (var cert in certificates)
        {
            if (await _unitOfWork.Workspaces.CanUserViewAsync(cert.WorkspaceId, _currentUser.UserId.Value, cancellationToken))
            {
                accessibleCertificates.Add(cert);
            }
        }

        var certificateDtos = accessibleCertificates.Select(MapToCertificateDto);
        return Result.Success(certificateDtos);
    }

    private CertificateDto MapToCertificateDto(Certificate certificate)
    {
        var dto = certificate.Adapt<CertificateDto>();
        return dto with
        {
            DaysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).Days,
            Status = certificate.Status.ToString(),
            FileFormat = certificate.FileFormat.ToString()
        };
    }
}