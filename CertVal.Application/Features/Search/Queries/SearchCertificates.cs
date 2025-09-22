using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Search.Queries;

public record SearchCertificatesQuery : IRequest<Result<PagedResult<CertificateDto>>>
{
    public string SearchTerm { get; init; } = string.Empty;
    public Guid? WorkspaceId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class SearchCertificatesQueryValidator : AbstractValidator<SearchCertificatesQuery>
{
    public SearchCertificatesQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters long")
            .MaximumLength(500).WithMessage("Search term must not exceed 500 characters");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");
    }
}

public class SearchCertificatesQueryHandler : IRequestHandler<SearchCertificatesQuery, Result<PagedResult<CertificateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SearchCertificatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<CertificateDto>>> Handle(SearchCertificatesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<PagedResult<CertificateDto>>("User not authenticated");

        IEnumerable<Certificate> certificates;

        if (request.WorkspaceId.HasValue)
        {
            if (!await CanAccessWorkspace(request.WorkspaceId.Value, cancellationToken))
                return Result.Failure<PagedResult<CertificateDto>>("Access denied to this workspace");

            certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId.Value, cancellationToken);
        }
        else
        {
            var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
            var workspaceIds = workspaces.Select(w => w.Id).ToList();

            certificates = new List<Certificate>();
            foreach (var workspaceId in workspaceIds)
            {
                var workspaceCerts = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId, cancellationToken);
                certificates = certificates.Concat(workspaceCerts);
            }
        }

        var filteredCertificates = FilterCertificatesBySearchTerm(certificates, request.SearchTerm);
        var totalCount = filteredCertificates.Count();

        var pagedCertificates = filteredCertificates
            .OrderBy(c => c.NotAfter)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var certificateDtos = pagedCertificates.Select(c => MapToCertificateDto(c)).ToList();
        var pagedResult = new PagedResult<CertificateDto>(certificateDtos, totalCount, request.PageNumber, request.PageSize);

        return Result.Success(pagedResult);
    }

    private IEnumerable<Certificate> FilterCertificatesBySearchTerm(IEnumerable<Certificate> certificates, string searchTerm)
    {
        var normalizedSearchTerm = searchTerm.ToLowerInvariant();

        return certificates.Where(c =>
            c.Subject.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
            c.Issuer.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(c.SerialNumber) && c.SerialNumber.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)) ||
            c.Thumbprint.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase) ||
            c.OriginalFileName.Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
        );
    }

    private CertificateDto MapToCertificateDto(Certificate certificate)
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

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}