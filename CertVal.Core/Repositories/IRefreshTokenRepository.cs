using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllActiveForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default);
}
