using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using Mapster;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography.X509Certificates;

namespace CertVal.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CertificateService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<CertificateDto>>> GetCertificatesAsync(CertificateFilterRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<PagedResult<CertificateDto>>("User not authenticated");

        IEnumerable<Certificate> certificates;

        if (request.WorkspaceId.HasValue)
        {
            // Check workspace access
            if (!await CanAccessWorkspace(request.WorkspaceId.Value, cancellationToken))
                return Result.Failure<PagedResult<CertificateDto>>("Access denied to this workspace");

            certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId.Value, cancellationToken);
        }
        else
        {
            // Get all certificates from user's accessible workspaces
            var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
            var workspaceIds = workspaces.Select(w => w.Id).ToList();

            certificates = new List<Certificate>();
            foreach (var workspaceId in workspaceIds)
            {
                var workspaceCerts = await _unitOfWork.Certificates.GetByWorkspaceAsync(workspaceId, cancellationToken);
                certificates = certificates.Concat(workspaceCerts);
            }
        }

        // Apply filters
        var filteredCertificates = ApplyFilters(certificates, request);

        // Apply sorting
        filteredCertificates = ApplySorting(filteredCertificates, request.SortBy, request.SortDescending);

        var totalCount = filteredCertificates.Count();

        // Apply pagination
        var pagedCertificates = filteredCertificates
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var certificateDtos = pagedCertificates.Select(c => MapToCertificateDto(c)).ToList();

        var pagedResult = new PagedResult<CertificateDto>(certificateDtos, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(pagedResult);
    }

    public async Task<Result<CertificateDto>> GetCertificateByIdAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure<CertificateDto>("Certificate not found");

        // Check workspace access
        if (!await CanAccessWorkspace(certificate.WorkspaceId, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this certificate");

        return Result.Success(MapToCertificateDto(certificate));
    }

    public async Task<Result<CertificateDto>> UploadCertificateAsync(UploadCertificateRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        // Check workspace access
        if (!await CanAccessWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this workspace");

        // Validate file
        if (request.File == null || request.File.Length == 0)
            return Result.Failure<CertificateDto>("No file provided");

        var validationResult = ValidateCertificateFile(request.File);
        if (!validationResult.IsSuccess)
            return Result.Failure<CertificateDto>(validationResult.Error);

        try
        {
            // Read and parse certificate
            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream, cancellationToken);
            var certificateData = stream.ToArray();

            var parseResult = ParseCertificate(certificateData, request.File.FileName);
            if (!parseResult.IsSuccess)
                return Result.Failure<CertificateDto>(parseResult.Error);

            var parsedCert = parseResult.Value;

            // Check for duplicates
            var existingCert = await _unitOfWork.Certificates.GetByThumbprintAsync(parsedCert.Thumbprint, cancellationToken);
            if (existingCert != null)
                return Result.Failure<CertificateDto>("Certificate with this thumbprint already exists");

            // Save file to storage (implement file storage service)
            var filePath = await SaveCertificateFile(certificateData, request.File.FileName);

            // Create certificate entity
            var certificate = Certificate.Create(
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
                parsedCert.SerialNumber
            );

            await _unitOfWork.Certificates.AddAsync(certificate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(MapToCertificateDto(certificate));
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

        // Check workspace access and permissions
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

        // Filter to only certificates from accessible workspaces
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

        if (request.IsExpired.HasValue)
        {
            if (request.IsExpired.Value)
                query = query.Where(c => c.NotAfter <= DateTime.UtcNow);
            else
                query = query.Where(c => c.NotAfter > DateTime.UtcNow);
        }

        if (request.IsBundle.HasValue)
            query = query.Where(c => c.IsBundle == request.IsBundle.Value);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CertificateStatus>(request.Status, out var status))
            query = query.Where(c => c.Status == status);

        return query;
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
            IsExpired = certificate.IsExpired,
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

    private Result<ParsedCertificateInfo> ParseCertificate(byte[] certificateData, string fileName)
    {
        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var format = DetermineFormat(extension);

            X509Certificate2 cert;

            if (extension == ".p7b" || extension == ".p7c")
            {
                // Handle PKCS#7 bundles
                var collection = new X509Certificate2Collection();
                collection.Import(certificateData);

                if (collection.Count == 0)
                    return Result.Failure<ParsedCertificateInfo>("No certificates found in bundle");

                cert = collection[0]!; // Use first certificate for metadata
            }
            else
            {
                cert = new X509Certificate2(certificateData);
            }

            var parsedInfo = new ParsedCertificateInfo
            {
                Subject = cert.Subject,
                Issuer = cert.Issuer,
                SerialNumber = cert.SerialNumber,
                Thumbprint = cert.Thumbprint,
                NotBefore = cert.NotBefore.ToUniversalTime(),
                NotAfter = cert.NotAfter.ToUniversalTime(),
                Format = format
            };

            return Result.Success(parsedInfo);
        }
        catch (Exception ex)
        {
            return Result.Failure<ParsedCertificateInfo>($"Failed to parse certificate: {ex.Message}");
        }
    }

    private CertificateFormat DetermineFormat(string extension)
    {
        return extension switch
        {
            ".cer" => CertificateFormat.CER,
            ".crt" => CertificateFormat.CRT,
            ".pem" => CertificateFormat.PEM,
            ".der" => CertificateFormat.DER,
            ".p7b" => CertificateFormat.P7B,
            ".p7c" => CertificateFormat.P7C,
            ".pfx" => CertificateFormat.PFX,
            ".p12" => CertificateFormat.P12,
            _ => CertificateFormat.Unknown
        };
    }

    private async Task<string> SaveCertificateFile(byte[] data, string fileName)
    {
        // TODO: Implement proper file storage service (local storage, Azure Blob, AWS S3, etc.)
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

        // API tokens with write access can manage certificates
        if (_currentUser.IsApiClient &&
            (_currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.ReadWrite ||
             _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin))
            return await CanAccessWorkspace(workspaceId, cancellationToken);

        // Check workspace membership with appropriate role
        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
        if (membership != null)
            return membership.CanManageCertificates;

        // Check if user is workspace owner
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        return workspace?.OwnerId == _currentUser.UserId.Value;
    }
}