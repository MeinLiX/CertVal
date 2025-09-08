using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Email == email.ToLowerInvariant());

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token, cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token &&
                                     u.PasswordResetTokenExpiresAt > DateTime.UtcNow,
                                 cancellationToken);
    }
}

public class WorkspaceRepository : BaseRepository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.OwnerId == userId || w.Members.Any(m => m.UserId == userId))
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Workspace?> GetWithMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Members)
                .ThenInclude(m => m.User)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(w => w.Id == workspaceId &&
                          (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)),
                     cancellationToken);
    }

    public async Task<bool> CanUserAccessAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await IsUserMemberAsync(workspaceId, userId, cancellationToken);
    }
}

public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
{
    public CertificateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Certificate>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetExpiringAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await DbSet
            .Include(c => c.Workspace)
            .Where(c => c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Workspace)
            .Where(c => c.NotAfter <= DateTime.UtcNow)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<Certificate?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Thumbprint == thumbprint, cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetBundleContentsAsync(Guid parentCertificateId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ParentCertificateId == parentCertificateId)
            .OrderBy(c => c.Subject)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetWorkspaceCertificateCountAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.WorkspaceId == workspaceId, cancellationToken);
    }
}

public class NotificationRuleRepository : BaseRepository<NotificationRule>, INotificationRuleRepository
{
    public NotificationRuleRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NotificationRule>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(nr => nr.WorkspaceId == workspaceId)
            .OrderBy(nr => nr.DaysBeforeExpiry)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(nr => nr.IsEnabled)
            .Include(nr => nr.Workspace)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationRule>> GetRulesForCertificateExpiryAsync(DateTime expiryDate, CancellationToken cancellationToken = default)
    {
        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;

        return await DbSet
            .Where(nr => nr.IsEnabled && nr.DaysBeforeExpiry >= daysUntilExpiry)
            .Include(nr => nr.Workspace)
            .ToListAsync(cancellationToken);
    }
}

public class NotificationHistoryRepository : BaseRepository<NotificationHistory>, INotificationHistoryRepository
{
    public NotificationHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NotificationHistory>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nh => nh.NotificationRule)
            .Include(nh => nh.Certificate)
            .Where(nh => nh.Status == Core.Enums.NotificationStatus.Pending &&
                        nh.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(nh => nh.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationHistory>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nh => nh.NotificationRule)
            .Include(nh => nh.Certificate)
            .Where(nh => nh.Status == Core.Enums.NotificationStatus.Failed && nh.CanRetry)
            .OrderBy(nh => nh.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationHistory>> GetByCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nh => nh.NotificationRule)
            .Where(nh => nh.CertificateId == certificateId)
            .OrderByDescending(nh => nh.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationHistory?> GetLastNotificationAsync(Guid certificateId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(nh => nh.CertificateId == certificateId && nh.NotificationRuleId == ruleId)
            .OrderByDescending(nh => nh.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(notificationId, cancellationToken);
        if (notification != null)
        {
            notification.MarkAsSent();
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class WorkspaceMemberRepository : BaseRepository<WorkspaceMember>, IWorkspaceMemberRepository
{
    public WorkspaceMemberRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<WorkspaceMember>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.User)
            .Include(wm => wm.InvitedByUser)
            .Where(wm => wm.WorkspaceId == workspaceId)
            .OrderBy(wm => wm.User.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkspaceMember>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.Workspace)
            .Where(wm => wm.UserId == userId)
            .OrderBy(wm => wm.Workspace.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceMember?> GetMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(wm => wm.User)
            .Include(wm => wm.Workspace)
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId,
                               cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId,
                     cancellationToken);
    }
}

public class ApiTokenRepository : BaseRepository<ApiToken>, IApiTokenRepository
{
    public ApiTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<ApiToken>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(at => at.UserId == userId)
            .OrderByDescending(at => at.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(at => at.User)
            .FirstOrDefaultAsync(at => at.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<ApiToken?> GetActiveTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(at => at.User)
            .FirstOrDefaultAsync(at => at.TokenHash == tokenHash && at.IsValid,
                               cancellationToken);
    }

    public async Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        var token = await GetByIdAsync(tokenId, cancellationToken);
        if (token != null)
        {
            token.Revoke();
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await DbSet
            .Where(at => at.UserId == userId && at.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}