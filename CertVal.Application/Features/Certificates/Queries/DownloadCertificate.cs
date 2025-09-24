using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using MediatR;

namespace CertVal.Application.Features.Certificates.Queries;

public record DownloadCertificateQuery(Guid CertificateId) : IRequest<Result<(byte[] FileContents, string FileName, string ContentType)>>;

public class DownloadCertificateQueryHandler : IRequestHandler<DownloadCertificateQuery, Result<(byte[] FileContents, string FileName, string ContentType)>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICertificateStorageService _storageService;

    public DownloadCertificateQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICertificateStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storageService = storageService;
    }

    public async Task<Result<(byte[] FileContents, string FileName, string ContentType)>> Handle(DownloadCertificateQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<(byte[], string, string)>("User not authenticated");

        var certificate = await _unitOfWork.Certificates.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate == null)
            return Result.Failure<(byte[], string, string)>("Certificate not found");

        if (!await _unitOfWork.Workspaces.CanUserAccessAsync(certificate.WorkspaceId, _currentUser.UserId.Value, cancellationToken))
            return Result.Failure<(byte[], string, string)>("Access denied to this certificate");

        if (!await _storageService.CertificateExistsAsync(certificate.FilePath, cancellationToken))
        {
            return Result.Failure<(byte[], string, string)>("Certificate file not found in storage.");
        }

        try
        {
            var fileContents = await _storageService.GetCertificateAsync(certificate.FilePath, cancellationToken);

            var contentType = "application/octet-stream";

            return Result.Success((fileContents, certificate.OriginalFileName, contentType));
        }
        catch (Exception ex)
        {
            return Result.Failure<(byte[], string, string)>($"Failed to retrieve certificate file: {ex.Message}");
        }
    }
}