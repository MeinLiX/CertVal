using CertVal.Application.Common.Interfaces;
using StackExchange.Redis;

namespace CertVal.Infrastructure.Services;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsAllowedAsync(string key, TimeSpan duration)
    {
        var db = _redis.GetDatabase();
        // Try to set the key only if it doesn't exist.
        // If it sets successfully, it means we are allowed.
        // If it fails (already exists), we are rate limited.
        bool set = await db.StringSetAsync(key, "1", duration, When.NotExists);
        return set;
    }
}
