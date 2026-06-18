using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class MonitoredEndpointRepository : BaseRepository<MonitoredEndpoint>, IMonitoredEndpointRepository
{
    public MonitoredEndpointRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<MonitoredEndpoint>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.WorkspaceId == workspaceId)
            .OrderBy(e => e.Host)
            .ThenBy(e => e.Port)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(e => e.WorkspaceId == workspaceId, cancellationToken);
    }

    public async Task<IReadOnlyList<MonitoredEndpoint>> GetDueAsync(DateTime nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) return Array.Empty<MonitoredEndpoint>();

        // The due window depends on each row's CheckIntervalMinutes, which does not
        // translate reliably to SQL, so pull the (bounded) set of enabled endpoints
        // ordered by staleness and apply the precise IsDue check in memory.
        var enabled = await DbSet
            .Where(e => e.IsEnabled)
            .OrderBy(e => e.LastCheckedAt)
            .ToListAsync(cancellationToken);

        return enabled
            .Where(e => e.IsDue(nowUtc))
            .Take(batchSize)
            .ToList();
    }
}
