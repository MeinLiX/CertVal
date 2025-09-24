using CertVal.EmailService.Services;

namespace CertVal.EmailService;

public class EmailWorkerService : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<EmailWorkerService> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public EmailWorkerService(
        IRabbitMqService rabbitMqService,
        ILogger<EmailWorkerService> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Email Worker Service starting...");

            await _rabbitMqService.StartConsumingAsync(stoppingToken);
            _logger.LogInformation("Email Worker Service started successfully");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email Worker Service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Email Worker Service failed to start");
            _hostApplicationLifetime.StopApplication();
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Worker Service is stopping...");

        try
        {
            await _rabbitMqService.StopConsumingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping RabbitMQ service");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Email Worker Service stopped");
    }
}