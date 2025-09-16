using CertVal.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Services;

public class CertificateExpiryCheckerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificateExpiryCheckerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public CertificateExpiryCheckerService(
        IServiceProvider serviceProvider,
        ILogger<CertificateExpiryCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delaySeconds = 30;
        _logger.LogInformation($"Certificate Expiry Checker Service started with {delaySeconds} seconds delay");

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCertificateExpiryAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Certificate Expiry Checker Service stopping");
                break;
            }
            catch (Exception ex)
            {
                var delayPauseMinutes = 30;
                _logger.LogError(ex, $"Error during certificate expiry check, service delay {delayPauseMinutes}");
                await Task.Delay(TimeSpan.FromMinutes(delayPauseMinutes), stoppingToken);
            }
        }
    }

    private async Task CheckCertificateExpiryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiringCertificates = await unitOfWork.Certificates.GetExpiringAsync(90, cancellationToken);

        if (!expiringCertificates.Any())
        {
            _logger.LogDebug("No certificates expiring in next 90 days");
            return;
        }

        var processedCount = 0;
        var eventCount = 0;

        foreach (var certificate in expiringCertificates)
        {
            try
            {
                var eventsBefore = certificate.DomainEvents.Count;
                certificate.CheckExpiry();

                processedCount++;
                eventCount += certificate.DomainEvents.Count - eventsBefore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking certificate {CertificateId} ({Subject})",
                    certificate.Id, certificate.Subject);
            }
        }

        if (processedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Processed {Count} certificates, triggered {Events} events",
                processedCount, eventCount);
        }
    }
}