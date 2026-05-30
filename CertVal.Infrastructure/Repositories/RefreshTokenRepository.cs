using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var activeTokens = await DbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(reason: reason);
        }
    }
}
