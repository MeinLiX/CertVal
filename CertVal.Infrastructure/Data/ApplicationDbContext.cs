using CertVal.Core.Entities;
using CertVal.Core.Events;
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
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<DomainEvent>();

        var domainEventTypes = typeof(DomainEvent).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DomainEvent)));

        foreach (var eventType in domainEventTypes)
        {
            modelBuilder.Ignore(eventType);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != Core.Enums.UserStatus.Deleted);
        modelBuilder.Entity<Certificate>().HasQueryFilter(c => c.Status != Core.Enums.CertificateStatus.Invalid);
        modelBuilder.Entity<WorkspaceMember>().HasQueryFilter(wm => wm.Status != Core.Enums.WorkspaceMemberStatus.Inactive);
        modelBuilder.Entity<ApiToken>().HasQueryFilter(at => at.User.Status != Core.Enums.UserStatus.Deleted);
        modelBuilder.Entity<Workspace>().HasQueryFilter(w => w.Owner.Status != Core.Enums.UserStatus.Deleted);
        modelBuilder.Entity<NotificationHistory>().HasQueryFilter(nh => nh.Certificate.Status != Core.Enums.CertificateStatus.Invalid);
        modelBuilder.Entity<NotificationRule>().HasQueryFilter(nr => nr.Workspace.Owner.Status != Core.Enums.UserStatus.Deleted);

        ConfigureAdditionalIndexes(modelBuilder);
    }

    private static void ConfigureAdditionalIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Certificate>()
            .HasIndex(c => new { c.WorkspaceId, c.NotAfter, c.Status })
            .HasDatabaseName("IX_Certificates_Workspace_Expiry_Status");

        modelBuilder.Entity<NotificationHistory>()
            .HasIndex(nh => new { nh.Status, nh.ScheduledAt, nh.RetryCount })
            .HasDatabaseName("IX_NotificationHistory_Processing");

        modelBuilder.Entity<ApiToken>()
            .HasIndex(at => new { at.UserId, at.IsActive, at.ExpiresAt })
            .HasDatabaseName("IX_ApiTokens_User_Active_Expires");

        modelBuilder.Entity<Certificate>()
            .HasIndex(c => new { c.Subject, c.Issuer })
            .HasDatabaseName("IX_Certificates_Subject_Issuer");
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().GetProperty("UpdatedAt") != null &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = Users.Where(u => u.Email == email.ToLowerInvariant());

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<Certificate>> GetExpiringCertificatesAsync(
        int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await Certificates
            .Include(c => c.Workspace)
                .ThenInclude(w => w.Owner)
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
            .Include(c => c.ParentCertificate)
            .Include(c => c.ChildCertificates)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationHistory>> GetPendingNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await NotificationHistory
            .Include(nh => nh.NotificationRule)
                .ThenInclude(nr => nr.Workspace)
                    .ThenInclude(w => w.Owner)
            .Include(nh => nh.Certificate)
            .Where(nh => nh.Status == Core.Enums.NotificationStatus.Pending &&
                        nh.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(nh => nh.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Workspace>> GetUserAccessibleWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Workspaces
            .Include(w => w.Owner)
            .Where(w => w.OwnerId == userId ||
                       w.Members.Any(m => m.UserId == userId && m.Status == Core.Enums.WorkspaceMemberStatus.Active))
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetWorkspaceCertificateCountsAsync(
        IEnumerable<Guid> workspaceIds,
        CancellationToken cancellationToken = default)
    {
        return await Certificates
            .Where(c => workspaceIds.Contains(c.WorkspaceId))
            .GroupBy(c => c.WorkspaceId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<List<Certificate>> GetCertificatesExpiringInDaysAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(days).Date;
        var nextDay = targetDate.AddDays(1);

        return await Certificates
            .Include(c => c.Workspace)
                .ThenInclude(w => w.Owner)
            .Include(c => c.Workspace)
                .ThenInclude(w => w.NotificationRules.Where(nr => nr.IsEnabled))
            .Where(c => c.NotAfter >= targetDate && c.NotAfter < nextDay)
            .ToListAsync(cancellationToken);
    }
}