using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Certificates.Queries;

public record GetCertificatesQuery : IRequest<Result<PagedResult<CertificateDto>>>
{
    public Guid? WorkspaceId { get; init; }
    public string? Subject { get; init; }
    public string? Issuer { get; init; }
    public DateTime? ExpiringBefore { get; init; }
    public DateTime? ExpiringAfter { get; init; }
    public CertificateStatusFilter StatusFilter { get; init; } = CertificateStatusFilter.All;
    public bool? IsBundle { get; init; }
    public string? Status { get; init; }
    public string? Tag { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetCertificatesQueryValidator : AbstractValidator<GetCertificatesQuery>
{
    public GetCertificatesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

        RuleFor(x => x.Subject)
            .MaximumLength(500).WithMessage("Subject must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Subject));

        RuleFor(x => x.Issuer)
            .MaximumLength(500).WithMessage("Issuer must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Issuer));

        RuleFor(x => x.ExpiringBefore)
            .GreaterThan(DateTime.MinValue).WithMessage("ExpiringBefore must be a valid date")
            .When(x => x.ExpiringBefore.HasValue);

        RuleFor(x => x.ExpiringAfter)
            .GreaterThan(DateTime.MinValue).WithMessage("ExpiringAfter must be a valid date")
            .When(x => x.ExpiringAfter.HasValue);

        RuleFor(x => x)
            .Must(x => !x.ExpiringBefore.HasValue || !x.ExpiringAfter.HasValue || x.ExpiringBefore >= x.ExpiringAfter)
            .WithMessage("ExpiringBefore must be greater than or equal to ExpiringAfter");

        RuleFor(x => x.StatusFilter)
            .IsInEnum().WithMessage("Status filter must be a valid value");
    }
}

public class GetCertificatesQueryHandler : IRequestHandler<GetCertificatesQuery, Result<PagedResult<CertificateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCertificatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<CertificateDto>>> Handle(GetCertificatesQuery request, CancellationToken cancellationToken)
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

        var filteredCertificates = ApplyFilters(certificates, request);
        filteredCertificates = ApplySorting(filteredCertificates, request.SortBy, request.SortDescending);

        var totalCount = filteredCertificates.Count();
        var pagedCertificates = filteredCertificates
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var certificateDtos = pagedCertificates.Select(c => MapToCertificateDto(c)).ToList();

        var pagedResult = new PagedResult<CertificateDto>(certificateDtos, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(pagedResult);
    }

    private IEnumerable<Certificate> ApplyFilters(IEnumerable<Certificate> certificates, GetCertificatesQuery request)
    {
        var query = certificates.AsQueryable();

        if (!string.IsNullOrEmpty(request.Subject))
            query = query.Where(c => c.Subject.Contains(request.Subject, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(request.Issuer))
            query = query.Where(c => c.Issuer.Contains(request.Issuer, StringComparison.OrdinalIgnoreCase));

        if (request.ExpiringBefore.HasValue)
            query = query.Where(c => c.NotAfter <= request.ExpiringBefore.Value);

        if (request.ExpiringAfter.HasValue)
            query = query.Where(c => c.NotAfter >= request.ExpiringAfter.Value);

        query = ApplyStatusFilter(query, request.StatusFilter);

        if (request.IsBundle.HasValue)
            query = query.Where(c => c.IsBundle == request.IsBundle.Value);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CertificateStatus>(request.Status, out var status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim();
            query = query.Where(c => c.Tags.Any(x => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)));
        }

        return query;
    }

    private static IQueryable<Certificate> ApplyStatusFilter(IQueryable<Certificate> query, CertificateStatusFilter statusFilter)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);

        return statusFilter switch
        {
            CertificateStatusFilter.All => query,
            CertificateStatusFilter.Valid => query.Where(c => c.NotAfter > expiringThreshold),
            CertificateStatusFilter.Expiring => query.Where(c => c.NotAfter > now && c.NotAfter <= expiringThreshold),
            CertificateStatusFilter.Expired => query.Where(c => c.NotAfter <= now),
            _ => query
        };
    }

    private IEnumerable<Certificate> ApplySorting(IEnumerable<Certificate> certificates, string? sortBy, bool sortDescending)
    {
        var query = certificates.AsQueryable();

        query = sortBy?.ToLower() switch
        {
            "subject" => sortDescending ? query.OrderByDescending(c => c.Subject) : query.OrderBy(c => c.Subject),
            "issuer" => sortDescending ? query.OrderByDescending(c => c.Issuer) : query.OrderBy(c => c.Issuer),
            "notbefore" => sortDescending ? query.OrderByDescending(c => c.NotBefore) : query.OrderBy(c => c.NotBefore),
            "createdat" => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => sortDescending ? query.OrderByDescending(c => c.NotAfter) : query.OrderBy(c => c.NotAfter)
        };

        return query;
    }

    private CertificateDto MapToCertificateDto(Certificate certificate)
    {
        var dto = certificate.Adapt<CertificateDto>();
        return dto with
        {
            DaysUntilExpiry = (certificate.NotAfter - DateTime.UtcNow).Days,
            Status = certificate.Status.ToString(),
            FileFormat = certificate.FileFormat.ToString(),
            OcspStatus = certificate.OcspStatus.ToString(),
            OcspLastCheckedAt = certificate.OcspLastCheckedAt,
            OcspResponderUrl = certificate.OcspResponderUrl,
            OcspRevocationReason = certificate.OcspRevocationReason,
            OcspRevokedAt = certificate.OcspRevokedAt,
            ChildCertificates = certificate.ChildCertificates.Select(MapToCertificateDto).ToList()
        };
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserViewAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}