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

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not available"));
            }

            if (!_connection.IsOpen)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is closed"));
            }

            using var model = _connection.CreateModel();
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex));
        }
    }
}
