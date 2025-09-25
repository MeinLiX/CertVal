using CertVal.EmailService.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CertVal.EmailService.HealthChecks;

public sealed class SmtpHealthCheck : IHealthCheck
{
    private readonly SmtpSettings _settings;

    public SmtpHealthCheck(IOptions<EmailServiceConfiguration> options)
    {
        _settings = options.Value.Smtp;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
                return HealthCheckResult.Unhealthy("SMTP host is not configured");

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.GetSslOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
            return HealthCheckResult.Healthy("SMTP server is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SMTP health check failed", ex);
        }
    }
}
