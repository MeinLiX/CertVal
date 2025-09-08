using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface IApiTokenRepository : IRepository<ApiToken>
{
    Task<IEnumerable<ApiToken>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<ApiToken?> GetActiveTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
