using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Mapster;

namespace CertVal.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public WorkspaceService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<WorkspaceDto>>> GetUserWorkspacesAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<PagedResult<WorkspaceDto>>("User not authenticated");

        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
        var workspaceList = workspaces.ToList();

        var totalCount = workspaceList.Count;
        var pagedWorkspaces = workspaceList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var workspaceDtos = new List<WorkspaceDto>();
        foreach (var workspace in pagedWorkspaces)
        {
            var dto = workspace.Adapt<WorkspaceDto>();
            dto = dto with
            {
                CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
                MemberCount = workspace.Members.Count + 1 // +1 for owner
            };
            workspaceDtos.Add(dto);
        }

        var pagedResult = new PagedResult<WorkspaceDto>(workspaceDtos, totalCount, pageNumber, pageSize);
        return Result.Success(pagedResult);
    }

    public async Task<Result<WorkspaceDto>> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<WorkspaceDto>("Workspace not found");

        if (!await CanAccessWorkspace(workspaceId, cancellationToken))
            return Result.Failure<WorkspaceDto>("Access denied to this workspace");

        var dto = workspace.Adapt<WorkspaceDto>();
        dto = dto with
        {
            CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
            MemberCount = workspace.Members.Count + 1 // +1 for owner
        };

        return Result.Success(dto);
    }

    public async Task<Result<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = Workspace.Create(
            request.Name,
            _currentUser.UserId.Value,
            request.Description
        );

        workspace.UpdateSettings(request.MaxCertificates, request.IsPublic, request.AllowMemberInvites);

        await _unitOfWork.Workspaces.AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdWorkspace = await _unitOfWork.Workspaces.GetByIdAsync(workspace.Id, cancellationToken);
        var dto = createdWorkspace!.Adapt<WorkspaceDto>() with
        {
            CertificateCount = 0,
            MemberCount = 1
        };

        return Result.Success(dto);
    }

    public async Task<Result<WorkspaceDto>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<WorkspaceDto>("Workspace not found");

        if (workspace.OwnerId != _currentUser.UserId.Value && !IsAdminUser())
            return Result.Failure<WorkspaceDto>("Access denied - only workspace owner can update settings");

        workspace.Update(request.Name, request.Description);
        workspace.UpdateSettings(request.MaxCertificates, request.IsPublic, request.AllowMemberInvites);

        await _unitOfWork.Workspaces.UpdateAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = workspace.Adapt<WorkspaceDto>() with
        {
            CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
            MemberCount = workspace.Members.Count + 1
        };

        return Result.Success(dto);
    }

    public async Task<Result> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (workspace.OwnerId != _currentUser.UserId.Value && !IsAdminUser())
            return Result.Failure("Access denied - only workspace owner can delete workspace");

        await _unitOfWork.Workspaces.DeleteAsync(workspaceId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }

    private bool IsAdminUser()
    {
        return _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin;
    }
}
