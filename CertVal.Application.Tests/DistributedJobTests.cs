using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Jobs;
using Xunit;

namespace CertVal.Application.Tests;

public class DistributedJobTests
{
    private sealed class FakeLockService(bool grant) : IDistributedLockService
    {
        public int AcquireCalls { get; private set; }
        public FakeHandle? LastHandle { get; private set; }

        public Task<IDistributedLockHandle?> TryAcquireAsync(string resource, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            AcquireCalls++;
            if (!grant) return Task.FromResult<IDistributedLockHandle?>(null);
            LastHandle = new FakeHandle();
            return Task.FromResult<IDistributedLockHandle?>(LastHandle);
        }
    }

    private sealed class FakeHandle : IDistributedLockHandle
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task RunIfAcquired_WhenLockGranted_RunsAction_AndReturnsTrue()
    {
        var locks = new FakeLockService(grant: true);
        var ran = false;

        var result = await DistributedJob.RunIfAcquiredAsync(locks, "cycle", TimeSpan.FromMinutes(1), _ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.True(result);
        Assert.True(ran);
        Assert.Equal(1, locks.AcquireCalls);
    }

    [Fact]
    public async Task RunIfAcquired_WhenLockGranted_ReleasesHandle()
    {
        var locks = new FakeLockService(grant: true);

        await DistributedJob.RunIfAcquiredAsync(locks, "cycle", TimeSpan.FromMinutes(1), _ => Task.CompletedTask);

        Assert.NotNull(locks.LastHandle);
        Assert.True(locks.LastHandle!.Disposed);
    }

    [Fact]
    public async Task RunIfAcquired_WhenLockNotGranted_SkipsAction_AndReturnsFalse()
    {
        var locks = new FakeLockService(grant: false);
        var ran = false;

        var result = await DistributedJob.RunIfAcquiredAsync(locks, "cycle", TimeSpan.FromMinutes(1), _ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.False(result);
        Assert.False(ran);
    }

    [Fact]
    public async Task RunIfAcquired_ReleasesHandle_EvenWhenActionThrows()
    {
        var locks = new FakeLockService(grant: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DistributedJob.RunIfAcquiredAsync(locks, "cycle", TimeSpan.FromMinutes(1), _ =>
                throw new InvalidOperationException("boom")));

        Assert.True(locks.LastHandle!.Disposed);
    }
}
