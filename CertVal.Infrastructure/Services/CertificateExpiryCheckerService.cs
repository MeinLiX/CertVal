using CertVal.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Services;
public class CertificateExpiryCheckerService : BackgroundService, ICertificateExpiryChecker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificateExpiryCheckerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    private readonly Lock _sleepLock = new();
    private CancellationTokenSource? _sleepCts;

    public CertificateExpiryCheckerService(
        IServiceProvider serviceProvider,
        ILogger<CertificateExpiryCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task TriggerCheckNowAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Immediate certificate expiry check requested");
        CancelSleep();
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delaySeconds = 30;
        _logger.LogInformation($"Certificate Expiry Checker Service started with {delaySeconds} seconds delay");

        if (!await DelayCancellableAsync(TimeSpan.FromSeconds(delaySeconds), stoppingToken))
        {
            _logger.LogInformation("Startup delay canceled, running immediate check");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCertificateExpiryAsync(stoppingToken);

                var waited = await DelayCancellableAsync(_checkInterval, stoppingToken);
                if (!waited && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Delay canceled by trigger; running immediate next cycle");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Certificate Expiry Checker Service stopping");
                break;
            }
            catch (Exception ex)
            {
                var delayPauseMinutes = 30;
                _logger.LogError(ex, $"Error during certificate expiry check, service delay {delayPauseMinutes}");

                await DelayCancellableAsync(TimeSpan.FromMinutes(delayPauseMinutes), stoppingToken);
            }
        }
    }

    private async Task<bool> DelayCancellableAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
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

        try
        {
            cts?.Cancel();
        }
        catch
        {
        }
    }

    private async Task CheckCertificateExpiryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ICertificateExpiryProcessor>();
        await processor.ProcessExpiryAsync(cancellationToken);
    }
}