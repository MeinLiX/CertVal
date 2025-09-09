using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;

namespace CertVal.Application.Services;

public interface IWorkspaceService
{
    Task<Result<PagedResult<WorkspaceDto>>> GetUserWorkspacesAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceDto>> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceDto>> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<Result<WorkspaceDto>> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}