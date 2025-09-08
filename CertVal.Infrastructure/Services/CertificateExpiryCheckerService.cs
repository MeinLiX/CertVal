using CertVal.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Services;

public class CertificateExpiryCheckerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificateExpiryCheckerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public CertificateExpiryCheckerService(
        IServiceProvider serviceProvider,
        ILogger<CertificateExpiryCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certificate Expiry Checker Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCertificateExpiryAsync(stoppingToken);

                _logger.LogDebug("Certificate expiry check completed. Next check in {Interval}", _checkInterval);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Certificate Expiry Checker Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during certificate expiry check");

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task CheckCertificateExpiryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _logger.LogDebug("Starting certificate expiry check");

        var expiringCertificates = await unitOfWork.Certificates.GetExpiringAsync(90, cancellationToken);

        if (!expiringCertificates.Any())
        {
            _logger.LogDebug("No certificates found expiring in the next 90 days");
            return;
        }

        _logger.LogInformation("Found {Count} certificates expiring in the next 90 days", expiringCertificates.Count());

        var processedCount = 0;
        var eventTriggeredCount = 0;

        foreach (var certificate in expiringCertificates)
        {
            try
            {
                certificate.CheckExpiry();
                processedCount++;

                if (certificate.DomainEvents.Any())
                {
                    eventTriggeredCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiry for certificate {CertificateId} ({Subject})",
                    certificate.Id, certificate.Subject);
            }
        }

        if (processedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Certificate expiry check completed. Processed: {ProcessedCount}, Events triggered: {EventCount}",
                processedCount, eventTriggeredCount);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Certificate Expiry Checker Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}