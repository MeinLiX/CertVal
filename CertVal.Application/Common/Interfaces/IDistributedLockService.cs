namespace CertVal.Application.Common.Interfaces;

/// <summary>
/// A held distributed lock. Disposing releases the lock (best-effort); the
/// underlying lock also auto-expires after its TTL so a crashed holder cannot
/// block the resource forever.
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
}

/// <summary>
/// Coordinates mutually-exclusive work across application instances so that
/// periodic background cycles (OCSP, expiry) are not run concurrently by
/// multiple replicas.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire the named lock. Returns a handle when acquired, or
    /// <c>null</c> when the lock is currently held by someone else.
    /// </summary>
    Task<IDistributedLockHandle?> TryAcquireAsync(string resource, TimeSpan ttl, CancellationToken cancellationToken = default);
}
