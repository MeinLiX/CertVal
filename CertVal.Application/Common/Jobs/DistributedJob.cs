using CertVal.Application.Common.Interfaces;

namespace CertVal.Application.Common.Jobs;

/// <summary>
/// Helper for running a unit of background work guarded by a distributed lock,
/// so only one application instance executes a given cycle at a time. Kept pure
/// (no infrastructure dependencies) so the guard behaviour is unit-testable.
/// </summary>
public static class DistributedJob
{
    /// <summary>
    /// Runs <paramref name="action"/> only if the named lock can be acquired.
    /// Returns <c>true</c> if the action ran, <c>false</c> if the lock was held
    /// by another instance and the work was skipped. The lock is always released
    /// when the action completes.
    /// </summary>
    public static async Task<bool> RunIfAcquiredAsync(
        IDistributedLockService locks,
        string resource,
        TimeSpan ttl,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await using var handle = await locks.TryAcquireAsync(resource, ttl, cancellationToken);
        if (handle is null)
            return false;

        await action(cancellationToken);
        return true;
    }
}
