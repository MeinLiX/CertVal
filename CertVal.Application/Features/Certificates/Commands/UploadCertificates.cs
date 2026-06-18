using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.Common.Telemetry;
using CertVal.Application.Configuration;
using CertVal.Application.DTOs;
using CertVal.Application.Services;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CertVal.Application.Features.Certificates.Commands;

public record UploadCertificatesCommand(Guid WorkspaceId, IEnumerable<IFormFile> Files) : IRequest<Result<BulkUploadResultDto>>;

public class UploadCertificatesCommandValidator : AbstractValidator<UploadCertificatesCommand>
{
    public UploadCertificatesCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.Files)
            .NotNull().WithMessage("Files are required")
            .Must(files => files.Any()).WithMessage("At least one file must be provided");

        RuleForEach(x => x.Files)
            .Must(BeValidCertificateFile).WithMessage("Invalid certificate file")
            .Must(BeWithinSizeLimit).WithMessage("File size exceeds 10 MB limit");
    }

    private static bool BeValidCertificateFile(IFormFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileName))
            return false;

        var allowedExtensions = new[] { ".cer", ".crt", ".pem", ".der", ".p7b", ".p7c", ".pfx", ".p12" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }

    private static bool BeWithinSizeLimit(IFormFile file)
    {
        return file?.Length <= 10 * 1024 * 1024; // 10 MB
    }
}

public class UploadCertificatesCommandHandler : IRequestHandler<UploadCertificatesCommand, Result<BulkUploadResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICertificateProcessorService _certificateProcessor;
    private readonly ICertificateStorageService _storageService;
    private readonly CertificateStorageConfiguration _config;

    public UploadCertificatesCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateProcessorService certificateProcessor,
        ICertificateStorageService storageService,
        IOptions<CertificateStorageConfiguration> config)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _certificateProcessor = certificateProcessor;
        _storageService = storageService;
        _config = config.Value;
    }

    public async Task<Result<BulkUploadResultDto>> Handle(UploadCertificatesCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<BulkUploadResultDto>("User not authenticated");

        if (!await _unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, _currentUser.UserId.Value, cancellationToken))
            return Result.Failure<BulkUploadResultDto>("Access denied to this workspace");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<BulkUploadResultDto>("Workspace not found");

        if (request.Files == null || !request.Files.Any())
            return Result.Failure<BulkUploadResultDto>("No files provided");

        await _storageService.EnsureBucketExistsAsync(cancellationToken);

        var results = new List<BulkUploadItemResult>();
        var successCount = 0;
        var skippedCount = 0;
        var failureCount = 0;

        var existingCertificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var existingThumbprints = existingCertificates.Select(c => c.Thumbprint).ToHashSet();
        var existingIssuerSerials = existingCertificates
            .Where(c => !string.IsNullOrWhiteSpace(c.SerialNumber))
            .Select(c => (c.Issuer, c.SerialNumber!))
            .ToHashSet();
        var currentCertificateCount = existingCertificates.Count();

        bool IsDuplicate(Common.Models.ParsedCertificateInfo cert) =>
            existingThumbprints.Contains(cert.Thumbprint) ||
            (!string.IsNullOrWhiteSpace(cert.SerialNumber) &&
             existingIssuerSerials.Contains((cert.Issuer, cert.SerialNumber!)));

        void TrackAdded(Common.Models.ParsedCertificateInfo cert)
        {
            existingThumbprints.Add(cert.Thumbprint);
            if (!string.IsNullOrWhiteSpace(cert.SerialNumber))
                existingIssuerSerials.Add((cert.Issuer, cert.SerialNumber!));
        }

        foreach (var file in request.Files)
        {
            try
            {
                var validationResult = ValidateCertificateFile(file);
                if (!validationResult.IsSuccess)
                {
                    results.Add(new BulkUploadItemResult { FileName = file.FileName, Success = false, ErrorMessage = validationResult.Error, IsSkipped = false });
                    failureCount++;
                    continue;
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, cancellationToken);
                var certificateData = stream.ToArray();

                var parseResult = await _certificateProcessor.ProcessCertificateAsync(certificateData, file.FileName, cancellationToken);

                if (!parseResult.IsSuccess)
                {
                    results.Add(new BulkUploadItemResult { FileName = file.FileName, Success = false, ErrorMessage = parseResult.Error, IsSkipped = false });
                    failureCount++;
                    continue;
                }

                var parsedCertificates = parseResult.Value.ToList();
                var newCertificates = parsedCertificates.Where(cert => !IsDuplicate(cert)).ToList();

                if (!newCertificates.Any())
                {
                    results.Add(new BulkUploadItemResult { FileName = file.FileName, Success = false, ErrorMessage = "All certificates in this file already exist in the workspace", IsSkipped = true });
                    skippedCount++;
                    continue;
                }

                var countToAdd = parsedCertificates.Count > 1 ? 1 + newCertificates.Count : 1;
                if (currentCertificateCount + countToAdd > workspace.MaxCertificates)
                {
                    results.Add(new BulkUploadItemResult
                    {
                        FileName = file.FileName,
                        Success = false,
                        ErrorMessage = $"Workspace certificate limit reached (Max: {workspace.MaxCertificates})",
                        IsSkipped = true
                    });
                    skippedCount++;
                    continue;
                }

                Certificate? parentCertificate = null;

                if (parsedCertificates.Count > 1)
                {
                    parentCertificate = Certificate.Create(
                        request.WorkspaceId,
                        $"Bundle: {file.FileName}",
                        "Certificate Bundle",
                        $"BUNDLE_{Guid.NewGuid():N}",
                        parsedCertificates.Min(c => c.NotBefore),
                        parsedCertificates.Max(c => c.NotAfter),
                        file.FileName,
                        string.Empty,
                        newCertificates.First().Format,
                        file.Length,
                        null,
                        null,
                        true);

                    var objectKey = await _storageService.StoreCertificateAsync(
                        request.WorkspaceId,
                        parentCertificate.Id,
                        file.FileName,
                        certificateData,
                        cancellationToken);

                    parentCertificate.UpdateFilePath(objectKey);
                    await _unitOfWork.Certificates.AddAsync(parentCertificate, cancellationToken);

                    foreach (var parsedCert in newCertificates)
                    {
                        var childCert = Certificate.Create(
                            request.WorkspaceId,
                            parsedCert.Subject,
                            parsedCert.Issuer,
                            parsedCert.Thumbprint,
                            parsedCert.NotBefore,
                            parsedCert.NotAfter,
                            file.FileName,
                            objectKey,
                            parsedCert.Format,
                            file.Length,
                            parsedCert.SerialNumber,
                            parentCertificate.Id);
                        await _unitOfWork.Certificates.AddAsync(childCert, cancellationToken);
                        TrackAdded(parsedCert);
                    }
                }
                else
                {
                    var mainCertificate = newCertificates.First();

                    parentCertificate = Certificate.Create(
                        request.WorkspaceId,
                        mainCertificate.Subject,
                        mainCertificate.Issuer,
                        mainCertificate.Thumbprint,
                        mainCertificate.NotBefore,
                        mainCertificate.NotAfter,
                        file.FileName,
                        string.Empty,
                        mainCertificate.Format,
                        file.Length,
                        mainCertificate.SerialNumber);

                    var objectKey = await _storageService.StoreCertificateAsync(
                        request.WorkspaceId,
                        parentCertificate.Id,
                        file.FileName,
                        certificateData,
                        cancellationToken);

                    parentCertificate.UpdateFilePath(objectKey);
                    await _unitOfWork.Certificates.AddAsync(parentCertificate, cancellationToken);
                    TrackAdded(mainCertificate);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                results.Add(new BulkUploadItemResult { FileName = file.FileName, Success = true, CertificateId = parentCertificate.Id, Subject = parentCertificate.Subject, IsSkipped = false });
                successCount++;
                currentCertificateCount += countToAdd;
                CertValDiagnostics.CertificatesUploaded.Add(1);
            }
            catch (Exception ex)
            {
                results.Add(new BulkUploadItemResult { FileName = file.FileName, Success = false, ErrorMessage = $"Unexpected error: {ex.Message}", IsSkipped = false });
                failureCount++;
            }
        }

        return Result.Success(new BulkUploadResultDto
        {
            TotalFiles = request.Files.Count(),
            SuccessCount = successCount,
            SkippedCount = skippedCount,
            FailureCount = failureCount,
            Results = results
        });
    }

    private Result ValidateCertificateFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".cer", ".crt", ".pem", ".der", ".p7b", ".p7c", ".pfx", ".p12" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
            return Result.Failure($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        if (file.Length > _config.MaxFileSize)
            return Result.Failure($"File size exceeds {_config.MaxFileSize / (1024 * 1024)} MB limit");

        return Result.Success();
    }
}