namespace CertVal.EmailService.Services;

public interface IRabbitMqService : IAsyncDisposable
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
    Task StopConsumingAsync();
}