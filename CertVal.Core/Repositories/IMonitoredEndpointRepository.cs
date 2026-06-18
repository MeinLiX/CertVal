using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IMonitoredEndpointRepository : IRepository<MonitoredEndpoint>
{
    Task<IEnumerable<MonitoredEndpoint>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonitoredEndpoint>> GetDueAsync(DateTime nowUtc, int batchSize, CancellationToken cancellationToken = default);
}
