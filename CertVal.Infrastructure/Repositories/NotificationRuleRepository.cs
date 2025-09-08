using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class NotificationRuleRepository : BaseRepository<NotificationRule>, INotificationRuleRepository
{
    public NotificationRuleRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NotificationRule>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nr => nr.Workspace)
            .Where(nr => nr.WorkspaceId == workspaceId)
            .OrderBy(nr => nr.DaysBeforeExpiry)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(nr => nr.Workspace)
                .ThenInclude(w => w.Owner)
            .Where(nr => nr.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationRule>> GetRulesForCertificateExpiryAsync(DateTime expiryDate, CancellationToken cancellationToken = default)
    {
        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;

        return await DbSet
            .Include(nr => nr.Workspace)
                .ThenInclude(w => w.Owner)
            .Where(nr => nr.IsEnabled && nr.DaysBeforeExpiry >= daysUntilExpiry)
            .ToListAsync(cancellationToken);
    }
}
