using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertVal.Infrastructure.Repositories;

public class ApiTokenRepository : BaseRepository<ApiToken>, IApiTokenRepository
{
    public ApiTokenRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<ApiToken>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(at => at.User)
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
            .FirstOrDefaultAsync(at => at.TokenHash == tokenHash 
                                    && at.IsActive 
                                    && (at.ExpiresAt == null || at.ExpiresAt > DateTime.UtcNow),
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