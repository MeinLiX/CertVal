using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Application.Services;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CertVal.Application.Features.Certificates.Commands;

public record UploadUpdatedCertificateCommand(Guid WorkspaceId, Guid PreviousCertificateId, IFormFile File) : IRequest<Result<CertificateDto>>;

public class UploadUpdatedCertificateCommandValidator : AbstractValidator<UploadUpdatedCertificateCommand>
{
    public UploadUpdatedCertificateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.PreviousCertificateId).NotEmpty();
        RuleFor(x => x.File).NotNull().Must(BeValidCertificateFile).WithMessage("Invalid certificate file");
    }

    private static bool BeValidCertificateFile(IFormFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileName))
            return false;

        var allowedExtensions = new[] { ".cer", ".crt", ".pem", ".der", ".p7b", ".p7c", ".pfx", ".p12" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}

public class UploadUpdatedCertificateCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ICertificateProcessorService certificateProcessor,
    ICertificateStorageService storageService) : IRequestHandler<UploadUpdatedCertificateCommand, Result<CertificateDto>>
{
    public async Task<Result<CertificateDto>> Handle(UploadUpdatedCertificateCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<CertificateDto>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<CertificateDto>("Access denied to this workspace");

        var previousCertificate = await unitOfWork.Certificates.GetByIdAsync(request.PreviousCertificateId, cancellationToken);
        if (previousCertificate == null || previousCertificate.WorkspaceId != request.WorkspaceId)
            return Result.Failure<CertificateDto>("Previous certificate not found");

        var nextVersion = await unitOfWork.Certificates.GetNextVersionAsync(previousCertificate.Id, cancellationToken);
        if (nextVersion != null)
        {
            return Result.Failure<CertificateDto>("A newer version of this certificate already exists.");
        }

        await storageService.EnsureBucketExistsAsync(cancellationToken);

        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);
        var certificateData = stream.ToArray();

        var parseResult = await certificateProcessor.ProcessCertificateAsync(certificateData, request.File.FileName, cancellationToken);

        if (!parseResult.IsSuccess)
            return Result.Failure<CertificateDto>(parseResult.Error);

        var parsedCertificates = parseResult.Value.ToList();
        if (!parsedCertificates.Any())
            return Result.Failure<CertificateDto>("No valid certificates found in file");

        var parsedInfo = parsedCertificates.First();

        if (parsedInfo.Thumbprint == previousCertificate.Thumbprint)
            return Result.Failure<CertificateDto>("Uploaded certificate is identical to the previous version.");

        if (await unitOfWork.Certificates.ExistsByIssuerAndSerialAsync(
                request.WorkspaceId, parsedInfo.Issuer, parsedInfo.SerialNumber, cancellationToken))
        {
            return Result.Failure<CertificateDto>("A certificate with the same issuer and serial number already exists in this workspace.");
        }

        var newCertificate = Certificate.Create(
            request.WorkspaceId,
            parsedInfo.Subject,
            parsedInfo.Issuer,
            parsedInfo.Thumbprint,
            parsedInfo.NotBefore,
            parsedInfo.NotAfter,
            request.File.FileName,
            string.Empty, // Placeholder path
            parsedInfo.Format,
            request.File.Length,
            parsedInfo.SerialNumber
        );

        newCertificate.SetPreviousCertificate(previousCertificate.Id);

        var filePath = await storageService.StoreCertificateAsync(request.WorkspaceId, newCertificate.Id, request.File.FileName, certificateData, cancellationToken);
        newCertificate.UpdateFilePath(filePath);

        await unitOfWork.Certificates.AddAsync(newCertificate, cancellationToken);

        previousCertificate.ToggleSkipMonitoring(true);
        await unitOfWork.Certificates.UpdateAsync(previousCertificate, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new CertificateDto
        {
            Id = newCertificate.Id,
            WorkspaceId = newCertificate.WorkspaceId,
            Subject = newCertificate.Subject,
            Issuer = newCertificate.Issuer,
            SerialNumber = newCertificate.SerialNumber,
            Thumbprint = newCertificate.Thumbprint,
            NotBefore = newCertificate.NotBefore,
            NotAfter = newCertificate.NotAfter,
            OriginalFileName = newCertificate.OriginalFileName,
            FileFormat = newCertificate.FileFormat.ToString(),
            FileSize = newCertificate.FileSize,
            IsBundle = newCertificate.IsBundle,
            ParentCertificateId = newCertificate.ParentCertificateId,
            Status = newCertificate.Status.ToString(),
            IsExpired = newCertificate.NotAfter < DateTime.UtcNow,
            DaysUntilExpiry = (int)(newCertificate.NotAfter - DateTime.UtcNow).TotalDays,
            CreatedAt = newCertificate.CreatedAt,
            UpdatedAt = newCertificate.UpdatedAt,
            IsSkipped = newCertificate.IsSkipped,
            PreviousCertificateId = newCertificate.PreviousCertificateId
        };

        return Result.Success(dto);
    }
}
