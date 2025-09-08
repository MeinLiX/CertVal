using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class WorkspaceMemberRepository : BaseRepository<WorkspaceMember>, IWorkspaceMemberRepository
{
    public WorkspaceMemberRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<WorkspaceMember>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.User)
            .Include(wm => wm.InvitedByUser)
            .Include(wm => wm.Workspace)
            .Where(wm => wm.WorkspaceId == workspaceId)
            .OrderBy(wm => wm.User.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkspaceMember>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Owner)
            .Where(wm => wm.UserId == userId)
            .OrderBy(wm => wm.Workspace.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceMember?> GetMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.User)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Owner)
            .Include(wm => wm.InvitedByUser)
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId,
                               cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(wm => wm.WorkspaceId == workspaceId &&
                           wm.UserId == userId &&
                           wm.Status == Core.Enums.WorkspaceMemberStatus.Active,
                     cancellationToken);
    }
}
