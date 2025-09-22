using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using System.Text;

namespace CertVal.Application.Features.Exports.Queries;

public record ExportCertificatesAsCsvQuery : IRequest<Result<(byte[] fileContents, string fileName, string contentType)>>
{
    public Guid? WorkspaceId { get; init; }
    public bool IncludeExpired { get; init; } = true;
}

public class ExportCertificatesAsCsvQueryValidator : AbstractValidator<ExportCertificatesAsCsvQuery>
{
    public ExportCertificatesAsCsvQueryValidator()
    {
    }
}

public class ExportCertificatesAsCsvQueryHandler : IRequestHandler<ExportCertificatesAsCsvQuery, Result<(byte[], string, string)>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ExportCertificatesAsCsvQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<(byte[], string, string)>> Handle(ExportCertificatesAsCsvQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Result.Failure<(byte[], string, string)>("User not authenticated");

        var userWorkspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
        var accessibleWorkspaceIds = userWorkspaces.Select(w => w.Id).ToHashSet();

        if (request.WorkspaceId.HasValue)
        {
            if (!accessibleWorkspaceIds.Contains(request.WorkspaceId.Value))
                return Result.Failure<(byte[], string, string)>("Access denied to this workspace");
            accessibleWorkspaceIds = new HashSet<Guid> { request.WorkspaceId.Value };
        }

        var allCertificates = new List<Core.Entities.Certificate>();
        foreach (var wsId in accessibleWorkspaceIds)
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(wsId, cancellationToken);
            allCertificates.AddRange(certificates);
        }

        if (!request.IncludeExpired)
        {
            allCertificates = allCertificates.Where(c => c.NotAfter > DateTime.UtcNow).ToList();
        }

        var csv = GenerateCertificatesCsv(allCertificates);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"certificates_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return Result.Success((bytes, fileName, "text/csv"));
    }

    private static string GenerateCertificatesCsv(List<Core.Entities.Certificate> certificates)
    {
        var csv = new StringBuilder();

        csv.AppendLine("Subject,Issuer,Serial Number,Thumbprint,Not Before,Not After,Days Until Expiry,Original File Name,File Format,File Size,Is Bundle,Status,Is Expired,Created At");

        foreach (var cert in certificates.OrderBy(c => c.NotAfter))
        {
            var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).Days;

            csv.AppendLine($"\"{EscapeCsv(cert.Subject)}\"," +
                          $"\"{EscapeCsv(cert.Issuer)}\"," +
                          $"\"{cert.SerialNumber ?? ""}\"," +
                          $"\"{cert.Thumbprint}\"," +
                          $"{cert.NotBefore:yyyy-MM-dd HH:mm:ss}," +
                          $"{cert.NotAfter:yyyy-MM-dd HH:mm:ss}," +
                          $"{daysUntilExpiry}," +
                          $"\"{EscapeCsv(cert.OriginalFileName)}\"," +
                          $"{cert.FileFormat}," +
                          $"{cert.FileSize}," +
                          $"{cert.IsBundle}," +
                          $"{cert.Status}," +
                          $"{cert.IsExpired}," +
                          $"{cert.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains('"'))
            field = field.Replace("\"", "\"\"");
        return field;
    }
}