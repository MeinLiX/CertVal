using CertVal.Application.Common.Interfaces;
using StackExchange.Redis;

namespace CertVal.Infrastructure.Services;

/// <summary>
/// Redis-backed distributed lock using <c>SET key token NX PX ttl</c> for
/// acquisition and a token-checked Lua script for release, so a lock is only
/// released by its owner. Mirrors the proven primitive used by
/// <see cref="RedisRateLimitService"/>.
/// </summary>
public sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IDistributedLockHandle?> TryAcquireAsync(string resource, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"lock:{resource}";
        var token = Guid.NewGuid().ToString("N");

        var acquired = await db.StringSetAsync(key, token, ttl, When.NotExists);
        return acquired ? new RedisLockHandle(db, key, token) : null;
    }

    private sealed class RedisLockHandle : IDistributedLockHandle
    {
        // Release only if we still own the lock (token matches), avoiding the
        // race where our TTL expired and another holder acquired it.
        private const string ReleaseScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;
        private bool _released;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;

            try
            {
                await _db.ScriptEvaluateAsync(ReleaseScript, [_key], [_token]);
            }
            catch
            {
                // Best-effort release; the TTL guarantees the lock frees eventually.
            }
        }
    }
}
