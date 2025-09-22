using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IWorkspaceMemberRepository : IRepository<WorkspaceMember>
{
    Task<IEnumerable<WorkspaceMember>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkspaceMember>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WorkspaceMember?> GetMembershipAsync(Guid workspaceId, Guid userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<WorkspaceMember?> GetByInvitationTokenAsync(string token, CancellationToken cancellationToken = default);
}