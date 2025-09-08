using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class NotificationHistoryRepository : BaseRepository<NotificationHistory>, INotificationHistoryRepository
{
    public NotificationHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NotificationHistory>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nh => nh.NotificationRule)
                .ThenInclude(nr => nr.Workspace)
            .Include(nh => nh.Certificate)
                .ThenInclude(c => c.Workspace)
            .Where(nh => nh.Status == Core.Enums.NotificationStatus.Pending &&
                        nh.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(nh => nh.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationHistory>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nh => nh.NotificationRule)
                .ThenInclude(nr => nr.Workspace)
            .Include(nh => nh.Certificate)
                .ThenInclude(c => c.Workspace)
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
            .Include(nh => nh.NotificationRule)
            .Include(nh => nh.Certificate)
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
