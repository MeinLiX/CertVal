using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace CertVal.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICertificateProcessorService _certificateProcessor;

    public CertificateService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateProcessorService certificateProcessor)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _certificateProcessor = certificateProcessor;
    }

    public async Task<Result<PagedResult<CertificateDto>>> GetCertificatesAsync(CertificateFilterRequest request, CancellationToken cancellationToken = default)
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

    private IEnumerable<Certificate> ApplyFilters(IEnumerable<Certificate> certificates, CertificateFilterRequest request)
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

    public async Task<Result<CertificateDto>> GetCertificateByIdAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure<CertificateDto>("Certificate not found");

        if (!await CanAccessWorkspace(certificate.WorkspaceId, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this certificate");

        return Result.Success(MapToCertificateDto(certificate));
    }

    public async Task<Result<CertificateDto>> UploadCertificateAsync(UploadCertificateRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        if (!await CanAccessWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this workspace");

        if (request.File == null || request.File.Length == 0)
            return Result.Failure<CertificateDto>("No file provided");

        var validationResult = ValidateCertificateFile(request.File);
        if (!validationResult.IsSuccess)
            return Result.Failure<CertificateDto>(validationResult.Error);

        try
        {
            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream, cancellationToken);
            var certificateData = stream.ToArray();

            var parseResult = await _certificateProcessor.ProcessCertificateAsync(
                certificateData,
                request.File.FileName,
                cancellationToken);

            if (!parseResult.IsSuccess)
                return Result.Failure<CertificateDto>(parseResult.Error);

            var parsedCertificates = parseResult.Value.ToList();
            var mainCertificate = parsedCertificates.First();

            var existingCert = await _unitOfWork.Certificates.GetByThumbprintAsync(mainCertificate.Thumbprint, cancellationToken);
            if (existingCert != null)
                return Result.Failure<CertificateDto>("Certificate with this thumbprint already exists");

            var filePath = await SaveCertificateFile(certificateData, request.File.FileName);

            Certificate? parentCertificate = null;

            if (parsedCertificates.Count > 1)
            {
                parentCertificate = Certificate.Create(
                    request.WorkspaceId,
                    $"Bundle: {request.File.FileName}",
                    "Certificate Bundle",
                    $"BUNDLE_{Guid.NewGuid():N}",
                    parsedCertificates.Min(c => c.NotBefore),
                    parsedCertificates.Max(c => c.NotAfter),
                    request.File.FileName,
                    filePath,
                    mainCertificate.Format,
                    request.File.Length,
                    null,
                    null,
                    true
                );

                await _unitOfWork.Certificates.AddAsync(parentCertificate, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var childCertificates = new List<Certificate>();
                foreach (var parsedCert in parsedCertificates)
                {
                    var childCert = Certificate.Create(
                        request.WorkspaceId,
                        parsedCert.Subject,
                        parsedCert.Issuer,
                        parsedCert.Thumbprint,
                        parsedCert.NotBefore,
                        parsedCert.NotAfter,
                        request.File.FileName,
                        filePath,
                        parsedCert.Format,
                        request.File.Length,
                        parsedCert.SerialNumber,
                        parentCertificate.Id
                    );

                    childCertificates.Add(childCert);
                    await _unitOfWork.Certificates.AddAsync(childCert, cancellationToken);
                }

                Certificate.CreateBundle(parentCertificate.Id, request.WorkspaceId, childCertificates);
            }
            else
            {
                parentCertificate = Certificate.Create(
                    request.WorkspaceId,
                    mainCertificate.Subject,
                    mainCertificate.Issuer,
                    mainCertificate.Thumbprint,
                    mainCertificate.NotBefore,
                    mainCertificate.NotAfter,
                    request.File.FileName,
                    filePath,
                    mainCertificate.Format,
                    request.File.Length,
                    mainCertificate.SerialNumber
                );

                await _unitOfWork.Certificates.AddAsync(parentCertificate, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(MapToCertificateDto(parentCertificate));
        }
        catch (Exception ex)
        {
            return Result.Failure<CertificateDto>($"Error processing certificate: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure("Certificate not found");

        if (!await CanManageCertificates(certificate.WorkspaceId, cancellationToken))
            return Result.Failure("Access denied - insufficient permissions");

        await _unitOfWork.Certificates.DeleteAsync(certificateId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<CertificateDto>>> GetExpiringCertificatesAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<CertificateDto>>("User not authenticated");

        var certificates = await _unitOfWork.Certificates.GetExpiringAsync(daysAhead, cancellationToken);

        var accessibleCertificates = new List<Certificate>();
        foreach (var cert in certificates)
        {
            if (await CanAccessWorkspace(cert.WorkspaceId, cancellationToken))
            {
                accessibleCertificates.Add(cert);
            }
        }

        var certificateDtos = accessibleCertificates.Select(MapToCertificateDto);
        return Result.Success(certificateDtos);
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
            _ => sortDescending ? query.OrderByDescending(c => c.NotAfter) : query.OrderBy(c => c.NotAfter) // Default: NotAfter
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
            ChildCertificates = certificate.ChildCertificates.Select(MapToCertificateDto).ToList()
        };
    }

    private Result ValidateCertificateFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".cer", ".crt", ".pem", ".der", ".p7b", ".p7c", ".pfx", ".p12" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
            return Result.Failure($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        if (file.Length > 10 * 1024 * 1024)
            return Result.Failure("File size exceeds 10 MB limit");

        return Result.Success();
    }

    private async Task<string> SaveCertificateFile(byte[] data, string fileName)
    {
        var uploadsDir = Path.Combine("wwwroot", "uploads", "certificates");
        Directory.CreateDirectory(uploadsDir);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, data);
        return filePath;
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }

    private async Task<bool> CanManageCertificates(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        if (_currentUser.IsApiClient &&
            (_currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.ReadWrite ||
             _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin))
            return await CanAccessWorkspace(workspaceId, cancellationToken);

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
        if (membership != null)
            return membership.CanManageCertificates;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        return workspace?.OwnerId == _currentUser.UserId.Value;
    }
}