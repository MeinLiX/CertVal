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

    public DownloadCertificateQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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

        if (!File.Exists(certificate.FilePath))
        {
            return Result.Failure<(byte[], string, string)>("Certificate file not found on server.");
        }

        var fileContents = await File.ReadAllBytesAsync(certificate.FilePath, cancellationToken);
        var contentType = "application/octet-stream";

        return Result.Success((fileContents, certificate.OriginalFileName, contentType));
    }
}