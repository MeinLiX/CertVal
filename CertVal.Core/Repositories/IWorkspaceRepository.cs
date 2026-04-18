using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWithMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanUserAccessAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanUserViewAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
}
