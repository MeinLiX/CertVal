using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Certificates.Queries;

public record GetCertificateByIdQuery(Guid CertificateId) : IRequest<Result<CertificateDto>>;

public class GetCertificateByIdQueryValidator : AbstractValidator<GetCertificateByIdQuery>
{
    public GetCertificateByIdQueryValidator()
    {
        RuleFor(x => x.CertificateId)
            .NotEmpty().WithMessage("Certificate ID is required");
    }
}

public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, Result<CertificateDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCertificateByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CertificateDto>> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure<CertificateDto>("Certificate not found");

        if (!await CanAccessWorkspace(certificate.WorkspaceId, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this certificate");

        var dto = certificate.Adapt<CertificateDto>();
        dto = dto with
        {
            DaysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).Days,
            Status = certificate.Status.ToString(),
            FileFormat = certificate.FileFormat.ToString(),
            ChildCertificates = certificate.ChildCertificates.Select(c => c.Adapt<CertificateDto>()).ToList()
        };

        return Result.Success(dto);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}