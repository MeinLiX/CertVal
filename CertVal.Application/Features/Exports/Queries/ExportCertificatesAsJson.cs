using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace CertVal.Application.Features.Exports.Queries;

public record ExportCertificatesAsJsonQuery : IRequest<Result<(byte[] fileContents, string fileName, string contentType)>>
{
    public Guid? WorkspaceId { get; init; }
    public bool IncludeExpired { get; init; } = true;
}

public class ExportCertificatesAsJsonQueryValidator : AbstractValidator<ExportCertificatesAsJsonQuery>
{
    public ExportCertificatesAsJsonQueryValidator()
    {
    }
}

public class ExportCertificatesAsJsonQueryHandler : IRequestHandler<ExportCertificatesAsJsonQuery, Result<(byte[], string, string)>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ExportCertificatesAsJsonQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<(byte[], string, string)>> Handle(ExportCertificatesAsJsonQuery request, CancellationToken cancellationToken)
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
        var workspaceNames = new Dictionary<Guid, string>();

        foreach (var wsId in accessibleWorkspaceIds)
        {
            var workspace = userWorkspaces.First(w => w.Id == wsId);
            workspaceNames[wsId] = workspace.Name;

            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(wsId, cancellationToken);
            allCertificates.AddRange(certificates);
        }

        if (!request.IncludeExpired)
        {
            allCertificates = allCertificates.Where(c => c.NotAfter > DateTime.UtcNow).ToList();
        }

        var exportData = new
        {
            ExportedAt = DateTime.UtcNow,
            ExportedBy = _currentUser.Email,
            TotalCertificates = allCertificates.Count,
            IncludeExpired = request.IncludeExpired,
            Workspaces = workspaceNames,
            Certificates = allCertificates.Select(c => new
            {
                c.Id,
                c.WorkspaceId,
                WorkspaceName = workspaceNames[c.WorkspaceId],
                c.Subject,
                c.Issuer,
                c.SerialNumber,
                c.Thumbprint,
                c.NotBefore,
                c.NotAfter,
                c.OriginalFileName,
                FileFormat = c.FileFormat.ToString(),
                c.FileSize,
                c.IsBundle,
                c.ParentCertificateId,
                Status = c.Status.ToString(),
                IsExpired = c.IsExpired,
                DaysUntilExpiry = (c.NotAfter - DateTime.UtcNow).Days,
                c.CreatedAt,
                c.UpdatedAt
            }).OrderBy(c => c.NotAfter)
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"certificates_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        return Result.Success((bytes, fileName, "application/json"));
    }
}