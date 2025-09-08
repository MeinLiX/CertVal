using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class WorkspaceRepository : BaseRepository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Owner)
            .Where(w => w.OwnerId == userId ||
                       w.Members.Any(m => m.UserId == userId && m.Status == Core.Enums.WorkspaceMemberStatus.Active))
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Workspace?> GetWithMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Members.Where(m => m.Status == Core.Enums.WorkspaceMemberStatus.Active))
                .ThenInclude(m => m.User)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(w => w.Id == workspaceId &&
                          (w.OwnerId == userId ||
                           w.Members.Any(m => m.UserId == userId && m.Status == Core.Enums.WorkspaceMemberStatus.Active)),
                     cancellationToken);
    }

    public async Task<bool> CanUserAccessAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await IsUserMemberAsync(workspaceId, userId, cancellationToken);
    }

    public override async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Owner)
            .Include(w => w.Members.Where(m => m.Status == Core.Enums.WorkspaceMemberStatus.Active))
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }
}
