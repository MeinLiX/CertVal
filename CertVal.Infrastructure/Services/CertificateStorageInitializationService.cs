using CertVal.Application.Common.Interfaces;
using CertVal.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CertVal.Infrastructure.Services;

public class CertificateStorageInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificateStorageInitializationService> _logger;
    private readonly CertificateStorageConfiguration _config;

    public CertificateStorageInitializationService(
        IServiceProvider serviceProvider,
        ILogger<CertificateStorageInitializationService> logger,
        IOptions<CertificateStorageConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting certificate storage initialization service");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<ICertificateStorageService>();

            await storageService.EnsureBucketExistsAsync(stoppingToken);

            _logger.LogInformation("Certificate storage initialization completed successfully. Bucket '{BucketName}' is ready with workspace prefixes: {UseWorkspacePrefixes}",
                _config.BucketName, _config.UseWorkspacePrefixes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize certificate storage");
            throw;
        }
    }
}