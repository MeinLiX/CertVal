using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class CertificateRepository : BaseRepository<Certificate>, ICertificateRepository
{
    public CertificateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Certificate>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.ParentCertificate)
            .Include(c => c.ChildCertificates)
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetExpiringAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await DbSet
            .Include(c => c.Workspace)
                .ThenInclude(w => w.Owner)
            .Where(c => c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow && !c.IsSkipped)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Workspace)
                .ThenInclude(w => w.Owner)
            .Where(c => c.NotAfter <= DateTime.UtcNow && !c.IsSkipped)
            .OrderBy(c => c.NotAfter)
            .ToListAsync(cancellationToken);
    }

    public async Task<Certificate?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Workspace)
            .FirstOrDefaultAsync(c => c.Thumbprint == thumbprint, cancellationToken);
    }

    public async Task<Certificate?> GetByThumbprintInWorkspaceAsync(string thumbprint, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Workspace)
            .FirstOrDefaultAsync(c => c.Thumbprint == thumbprint && c.WorkspaceId == workspaceId, cancellationToken);
    }

    public async Task<bool> ExistsByIssuerAndSerialAsync(Guid workspaceId, string issuer, string? serialNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return false;

        return await DbSet.AnyAsync(c =>
            c.WorkspaceId == workspaceId &&
            c.Issuer == issuer &&
            c.SerialNumber == serialNumber,
            cancellationToken);
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

    public override async Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Workspace)
                .ThenInclude(w => w.Owner)
            .Include(c => c.ParentCertificate)
            .Include(c => c.ChildCertificates)
            .Include(c => c.NotificationHistory)
                .ThenInclude(nh => nh.NotificationRule)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Certificate?> GetNextVersionAsync(Guid previousCertificateId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.PreviousCertificateId == previousCertificateId, cancellationToken);
    }

    public async Task<IEnumerable<Certificate>> GetExpiringByWorkspacesAsync(IEnumerable<Guid> workspaceIds, int daysAhead, int limit, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await DbSet
            .Where(c => workspaceIds.Contains(c.WorkspaceId) && c.NotAfter <= cutoffDate && c.NotAfter > DateTime.UtcNow && !c.IsSkipped)
            .OrderBy(c => c.NotAfter)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Certificate>> GetForOcspCheckAsync(int batchSize, TimeSpan minCheckInterval, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) return Array.Empty<Certificate>();

        var now = DateTime.UtcNow;
        var dueCutoff = now - minCheckInterval;

        return await DbSet
            .Where(c => !c.IsBundle
                        && !c.IsSkipped
                        && c.Status != CertificateStatus.Revoked
                        && c.NotAfter > now
                        && (c.OcspLastCheckedAt == null || c.OcspLastCheckedAt < dueCutoff))
            .OrderBy(c => c.OcspLastCheckedAt)
            .ThenBy(c => c.NotAfter)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}