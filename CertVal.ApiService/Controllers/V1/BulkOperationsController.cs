using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using CertVal.Application.Services;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/bulk")]
[Authorize]
[Tags("Bulk Operations")]
public class BulkOperationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICertificateProcessorService _certificateProcessor;

    public BulkOperationsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateProcessorService certificateProcessor)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _certificateProcessor = certificateProcessor;
    }

    [HttpPost("certificates/upload")]
    [ProducesResponseType(typeof(BulkUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkUploadResultDto>> BulkUploadCertificates(
        [FromForm] BulkUploadCertificatesRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        if (!await CanAccessWorkspace(request.WorkspaceId))
            return Forbid();

        var results = new List<BulkUploadItemResult>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var file in request.Files)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                var certificateData = stream.ToArray();

                var parseResult = await _certificateProcessor.ProcessCertificateAsync(
                    certificateData, file.FileName);

                if (!parseResult.IsSuccess)
                {
                    results.Add(new BulkUploadItemResult
                    {
                        FileName = file.FileName,
                        Success = false,
                        ErrorMessage = parseResult.Error
                    });
                    failureCount++;
                    continue;
                }

                var parsedCertificates = parseResult.Value.ToList();
                var mainCertificate = parsedCertificates.First();

                var existingCert = await _unitOfWork.Certificates.GetByThumbprintAsync(mainCertificate.Thumbprint);
                if (existingCert != null)
                {
                    results.Add(new BulkUploadItemResult
                    {
                        FileName = file.FileName,
                        Success = false,
                        ErrorMessage = "Certificate with this thumbprint already exists"
                    });
                    failureCount++;
                    continue;
                }

                var filePath = await SaveCertificateFile(certificateData, file.FileName);

                Core.Entities.Certificate? certificate;

                if (parsedCertificates.Count > 1)
                {
                    certificate = Core.Entities.Certificate.Create(
                        request.WorkspaceId,
                        $"Bundle: {file.FileName}",
                        "Certificate Bundle",
                        $"BUNDLE_{Guid.NewGuid():N}",
                        parsedCertificates.Min(c => c.NotBefore),
                        parsedCertificates.Max(c => c.NotAfter),
                        file.FileName,
                        filePath,
                        mainCertificate.Format,
                        file.Length,
                        null,
                        null,
                        true
                    );

                    await _unitOfWork.Certificates.AddAsync(certificate);
                    await _unitOfWork.SaveChangesAsync();

                    foreach (var parsedCert in parsedCertificates)
                    {
                        var childCert = Core.Entities.Certificate.Create(
                            request.WorkspaceId,
                            parsedCert.Subject,
                            parsedCert.Issuer,
                            parsedCert.Thumbprint,
                            parsedCert.NotBefore,
                            parsedCert.NotAfter,
                            file.FileName,
                            filePath,
                            parsedCert.Format,
                            file.Length,
                            parsedCert.SerialNumber,
                            certificate.Id
                        );

                        await _unitOfWork.Certificates.AddAsync(childCert);
                    }
                }
                else
                {
                    certificate = Core.Entities.Certificate.Create(
                        request.WorkspaceId,
                        mainCertificate.Subject,
                        mainCertificate.Issuer,
                        mainCertificate.Thumbprint,
                        mainCertificate.NotBefore,
                        mainCertificate.NotAfter,
                        file.FileName,
                        filePath,
                        mainCertificate.Format,
                        file.Length,
                        mainCertificate.SerialNumber
                    );

                    await _unitOfWork.Certificates.AddAsync(certificate);
                }

                await _unitOfWork.SaveChangesAsync();

                results.Add(new BulkUploadItemResult
                {
                    FileName = file.FileName,
                    Success = true,
                    CertificateId = certificate.Id,
                    Subject = certificate.Subject
                });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new BulkUploadItemResult
                {
                    FileName = file.FileName,
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                });
                failureCount++;
            }
        }

        return Ok(new BulkUploadResultDto
        {
            TotalFiles = request.Files.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        });
    }

    [HttpDelete("certificates")]
    [ProducesResponseType(typeof(BulkDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkDeleteResultDto>> BulkDeleteCertificates(
        BulkDeleteCertificatesRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var results = new List<BulkDeleteItemResult>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var certificateId in request.CertificateIds)
        {
            try
            {
                var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId);
                if (certificate == null)
                {
                    results.Add(new BulkDeleteItemResult
                    {
                        CertificateId = certificateId,
                        Success = false,
                        ErrorMessage = "Certificate not found"
                    });
                    failureCount++;
                    continue;
                }

                if (!await CanAccessWorkspace(certificate.WorkspaceId))
                {
                    results.Add(new BulkDeleteItemResult
                    {
                        CertificateId = certificateId,
                        Success = false,
                        ErrorMessage = "Access denied"
                    });
                    failureCount++;
                    continue;
                }

                await _unitOfWork.Certificates.DeleteAsync(certificateId);

                results.Add(new BulkDeleteItemResult
                {
                    CertificateId = certificateId,
                    Success = true,
                    Subject = certificate.Subject
                });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new BulkDeleteItemResult
                {
                    CertificateId = certificateId,
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                });
                failureCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(new BulkDeleteResultDto
        {
            TotalCertificates = request.CertificateIds.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        });
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value);
    }

    private static async Task<string> SaveCertificateFile(byte[] data, string fileName)
    {
        var uploadsDir = Path.Combine("wwwroot", "uploads", "certificates");
        Directory.CreateDirectory(uploadsDir);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        await System.IO.File.WriteAllBytesAsync(filePath, data);
        return filePath;
    }
}