using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Jobs;
using CertVal.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CertVal.Infrastructure.Services;


public sealed class CertificateRevocationCheckService : BackgroundService, ICertificateRevocationChecker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<OcspCheckingConfiguration> _options;
    private readonly ILogger<CertificateRevocationCheckService> _logger;

    private readonly Lock _sleepLock = new();
    private CancellationTokenSource? _sleepCts;

    public CertificateRevocationCheckService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<OcspCheckingConfiguration> options,
        ILogger<CertificateRevocationCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public Task TriggerCheckNowAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Immediate OCSP revocation check requested");
        CancelSleep();
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = _options.CurrentValue;
        _logger.LogInformation(
            "Certificate Revocation Check Service started (cycle={Cycle}min, batch={Batch}, concurrency={Concurrency}, perCertInterval={PerCert}min)",
            config.CycleIntervalMinutes, config.BatchSize, config.MaxConcurrency, config.MinPerCertificateIntervalMinutes);

        await DelayCancellableAsync(TimeSpan.FromSeconds(Math.Max(0, config.StartupDelaySeconds)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCSP revocation cycle failed; backing off");
                await DelayCancellableAsync(TimeSpan.FromMinutes(5), stoppingToken);
            }

            var current = _options.CurrentValue;
            var cooldown = TimeSpan.FromSeconds(Math.Max(0, current.InterCycleCooldownSeconds));
            if (cooldown > TimeSpan.Zero)
                await DelayCancellableAsync(cooldown, stoppingToken);

            var cycle = TimeSpan.FromMinutes(Math.Max(1, current.CycleIntervalMinutes));
            await DelayCancellableAsync(cycle, stoppingToken);
        }

        _logger.LogInformation("Certificate Revocation Check Service stopping");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var locks = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var processor = scope.ServiceProvider.GetRequiredService<ICertificateRevocationProcessor>();

        var ran = await DistributedJob.RunIfAcquiredAsync(
            locks,
            "ocsp-revocation-cycle",
            TimeSpan.FromMinutes(15),
            processor.ProcessRevocationChecksAsync,
            cancellationToken);

        if (!ran)
            _logger.LogDebug("OCSP revocation cycle skipped: another instance holds the lock");
    }

    private async Task<bool> DelayCancellableAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        if (delay <= TimeSpan.Zero) return true;

        CancellationToken ct;
        lock (_sleepLock)
        {
            _sleepCts?.Dispose();
            _sleepCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            ct = _sleepCts.Token;
        }

        try
        {
            await Task.Delay(delay, ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void CancelSleep()
    {
        CancellationTokenSource? cts;
        lock (_sleepLock)
        {
            cts = _sleepCts;
        }

        try { cts?.Cancel(); } catch { }
    }
}
