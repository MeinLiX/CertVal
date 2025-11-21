using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace CertVal.EmailService.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConnection _connection;

    public RabbitMqHealthCheck(IConnection connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection is null)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection is not available");
            }

            if (!_connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection is closed");
            }

            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex);
        }
    }
}
