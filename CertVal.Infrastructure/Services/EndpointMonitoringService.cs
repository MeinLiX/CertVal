using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Jobs;
using CertVal.Application.Common.Tools;
using CertVal.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Services;

/// <summary>
/// Periodically scans due <see cref="Core.Entities.MonitoredEndpoint"/>s by
/// performing a live TLS handshake (via <see cref="ISslInspectionService"/>),
/// grading the result and recording the latest state. Guarded by a distributed
/// lock so only one instance scans at a time across replicas.
/// </summary>
public sealed class EndpointMonitoringService : BackgroundService
{
    private static readonly TimeSpan CycleInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 25;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EndpointMonitoringService> _logger;

    public EndpointMonitoringService(IServiceProvider serviceProvider, ILogger<EndpointMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Endpoint Monitoring Service started (cycle={Cycle}min, batch={Batch})",
            CycleInterval.TotalMinutes, BatchSize);

        // Small startup delay so the app is fully up before the first scan.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

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
                _logger.LogError(ex, "Endpoint monitoring cycle failed; backing off");
            }

            try { await Task.Delay(CycleInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("Endpoint Monitoring Service stopping");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var locks = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        var ran = await DistributedJob.RunIfAcquiredAsync(
            locks,
            "endpoint-monitoring-cycle",
            TimeSpan.FromMinutes(10),
            ct => ScanDueAsync(scope.ServiceProvider, ct),
            cancellationToken);

        if (!ran)
            _logger.LogDebug("Endpoint monitoring cycle skipped: another instance holds the lock");
    }

    private async Task ScanDueAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var unitOfWork = scopedProvider.GetRequiredService<IUnitOfWork>();
        var inspector = scopedProvider.GetRequiredService<ISslInspectionService>();

        var nowUtc = DateTime.UtcNow;
        var due = await unitOfWork.MonitoredEndpoints.GetDueAsync(nowUtc, BatchSize, cancellationToken);
        if (due.Count == 0)
        {
            _logger.LogDebug("Endpoint monitoring: no endpoints due");
            return;
        }

        _logger.LogInformation("Endpoint monitoring: scanning {Count} endpoint(s)", due.Count);

        foreach (var endpoint in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await inspector.InspectAsync(endpoint.Host, endpoint.Port, cancellationToken);
                var (grade, findings) = TlsGradeCalculator.Evaluate(result);

                endpoint.RecordResult(
                    reachable: result.Reachable,
                    grade: grade,
                    protocol: result.NegotiatedProtocol,
                    leafNotAfter: result.Leaf?.NotAfter,
                    leafSubject: result.Leaf?.Subject,
                    leafThumbprint: result.Leaf?.Sha256Thumbprint,
                    error: result.Error);

                await unitOfWork.MonitoredEndpoints.UpdateAsync(endpoint, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan endpoint {Host}:{Port}", endpoint.Host, endpoint.Port);
                endpoint.RecordResult(false, "F", null, null, null, null, ex.Message);
                await unitOfWork.MonitoredEndpoints.UpdateAsync(endpoint, cancellationToken);
            }
        }

        // Persists results and dispatches any EndpointCertificateChangedEvent
        // raised by RecordResult (which then lands in the workspace audit log).
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
