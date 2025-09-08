using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

    public bool EnsureDelete() => Database.EnsureDeleted();
    public bool EnsureCreate() => Database.EnsureCreated();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != Core.Enums.UserStatus.Deleted);
        modelBuilder.Entity<Certificate>().HasQueryFilter(c => c.Status != Core.Enums.CertificateStatus.Invalid);
        modelBuilder.Entity<WorkspaceMember>().HasQueryFilter(wm => wm.Status != Core.Enums.WorkspaceMemberStatus.Inactive);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().GetProperty("UpdatedAt") != null &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }


    public async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<List<Certificate>> GetExpiringCertificatesAsync(
        int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await Certificates
            .Include(c => c.Workspace)
            .Where(c => c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Certificate>> GetWorkspaceCertificatesAsync(
        Guid workspaceId,
        bool includeExpired = false,
        CancellationToken cancellationToken = default)
    {
        var query = Certificates
            .Where(c => c.WorkspaceId == workspaceId);

        if (!includeExpired)
        {
            query = query.Where(c => c.NotAfter > DateTime.UtcNow);
        }

        return await query
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationHistory>> GetPendingNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await NotificationHistory
            .Include(nh => nh.NotificationRule)
            .Include(nh => nh.Certificate)
            .Where(nh => nh.Status == Core.Enums.NotificationStatus.Pending &&
                        nh.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(nh => nh.ScheduledAt)
            .ToListAsync(cancellationToken);
    }
}