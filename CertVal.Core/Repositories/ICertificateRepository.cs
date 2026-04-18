using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface ICertificateRepository : IRepository<Certificate>
{
    Task<IEnumerable<Certificate>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Certificate>> GetExpiringAsync(int daysAhead = 30, CancellationToken cancellationToken = default);
    Task<IEnumerable<Certificate>> GetExpiredAsync(CancellationToken cancellationToken = default);
    Task<Certificate?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default);
    Task<Certificate?> GetByThumbprintInWorkspaceAsync(string thumbprint, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIssuerAndSerialAsync(Guid workspaceId, string issuer, string? serialNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Certificate>> GetBundleContentsAsync(Guid parentCertificateId, CancellationToken cancellationToken = default);
    Task<int> GetWorkspaceCertificateCountAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<Certificate?> GetNextVersionAsync(Guid previousCertificateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Certificate>> GetExpiringByWorkspacesAsync(IEnumerable<Guid> workspaceIds, int daysAhead, int limit, CancellationToken cancellationToken = default);
}