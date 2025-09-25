namespace CertVal.EmailService.Services.Abstractions;

public interface IRabbitMqService : IAsyncDisposable
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
    Task StopConsumingAsync();
}