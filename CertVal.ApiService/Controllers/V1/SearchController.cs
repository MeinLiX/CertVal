using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers.V1;

[ApiController]
[Route("api/v1/search")]
[Authorize]
[Tags("Search")]
public class SearchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SearchController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    [HttpGet("certificates")]
    [ProducesResponseType(typeof(PagedResult<CertificateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CertificateDto>>> SearchCertificates(
        [FromQuery] string? query = null,
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool? isExpired = null,
        [FromQuery] int? daysUntilExpiry = null,
        [FromQuery] string? format = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userWorkspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);
        var accessibleWorkspaceIds = userWorkspaces.Select(w => w.Id).ToHashSet();

        if (workspaceId.HasValue)
        {
            if (!accessibleWorkspaceIds.Contains(workspaceId.Value))
                return Forbid();

            accessibleWorkspaceIds = new HashSet<Guid> { workspaceId.Value };
        }

        var allCertificates = new List<Core.Entities.Certificate>();
        foreach (var wsId in accessibleWorkspaceIds)
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(wsId);
            allCertificates.AddRange(certificates);
        }

        var filteredCertificates = allCertificates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = query.ToLowerInvariant();
            filteredCertificates = filteredCertificates.Where(c =>
                c.Subject.ToLowerInvariant().Contains(searchTerm) ||
                c.Issuer.ToLowerInvariant().Contains(searchTerm) ||
                c.OriginalFileName.ToLowerInvariant().Contains(searchTerm) ||
                (c.SerialNumber != null && c.SerialNumber.ToLowerInvariant().Contains(searchTerm)));
        }

        if (isExpired.HasValue)
        {
            if (isExpired.Value)
                filteredCertificates = filteredCertificates.Where(c => c.NotAfter <= DateTime.UtcNow);
            else
                filteredCertificates = filteredCertificates.Where(c => c.NotAfter > DateTime.UtcNow);
        }

        if (daysUntilExpiry.HasValue)
        {
            var targetDate = DateTime.UtcNow.AddDays(daysUntilExpiry.Value);
            filteredCertificates = filteredCertificates.Where(c =>
                c.NotAfter <= targetDate && c.NotAfter > DateTime.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(format) && Enum.TryParse<Core.Enums.CertificateFormat>(format, true, out var certFormat))
        {
            filteredCertificates = filteredCertificates.Where(c => c.FileFormat == certFormat);
        }

        filteredCertificates = filteredCertificates.OrderBy(c => c.NotAfter);

        var totalCount = filteredCertificates.Count();
        var pagedCertificates = filteredCertificates
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var certificateDtos = pagedCertificates.Select(c => new CertificateDto
        {
            Id = c.Id,
            WorkspaceId = c.WorkspaceId,
            Subject = c.Subject,
            Issuer = c.Issuer,
            SerialNumber = c.SerialNumber,
            Thumbprint = c.Thumbprint,
            NotBefore = c.NotBefore,
            NotAfter = c.NotAfter,
            OriginalFileName = c.OriginalFileName,
            FileFormat = c.FileFormat.ToString(),
            FileSize = c.FileSize,
            IsBundle = c.IsBundle,
            ParentCertificateId = c.ParentCertificateId,
            Status = c.Status.ToString(),
            IsExpired = c.IsExpired,
            DaysUntilExpiry = (c.NotAfter - DateTime.UtcNow).Days,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

        var result = new PagedResult<CertificateDto>(certificateDtos, totalCount, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("workspaces")]
    [ProducesResponseType(typeof(IEnumerable<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<WorkspaceDto>>> SearchWorkspaces(
        [FromQuery] string? query = null)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = query.ToLowerInvariant();
            workspaces = workspaces.Where(w =>
                w.Name.ToLowerInvariant().Contains(searchTerm) ||
                (w.Description != null && w.Description.ToLowerInvariant().Contains(searchTerm)));
        }

        var workspaceDtos = new List<WorkspaceDto>();
        foreach (var workspace in workspaces)
        {
            var dto = new WorkspaceDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                OwnerId = workspace.OwnerId,
                Owner = new UserDto
                {
                    Id = workspace.Owner.Id,
                    Email = workspace.Owner.Email,
                    FirstName = workspace.Owner.FirstName,
                    LastName = workspace.Owner.LastName,
                    FullName = workspace.Owner.FullName,
                    IsEmailConfirmed = workspace.Owner.IsEmailConfirmed,
                    LastLoginAt = workspace.Owner.LastLoginAt,
                    Status = workspace.Owner.Status.ToString(),
                    CreatedAt = workspace.Owner.CreatedAt
                },
                MaxCertificates = workspace.MaxCertificates,
                IsPublic = workspace.IsPublic,
                AllowMemberInvites = workspace.AllowMemberInvites,
                CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id),
                MemberCount = workspace.Members.Count + 1, // +1 for owner
                CreatedAt = workspace.CreatedAt,
                UpdatedAt = workspace.UpdatedAt
            };
            workspaceDtos.Add(dto);
        }

        return Ok(workspaceDtos.OrderBy(w => w.Name));
    }
}