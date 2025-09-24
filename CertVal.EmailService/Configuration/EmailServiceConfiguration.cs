using MailKit.Security;

namespace CertVal.EmailService.Configuration;

public class EmailServiceConfiguration
{
    public const string SectionName = "EmailService";

    public SmtpSettings Smtp { get; set; } = new();
    public string SupportEmail { get; set; } = "support@certval.halerka.dev";
    public string CompanyName { get; set; } = "CertVal";
    public string BaseUrl { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? SslMode { get; set; } = null;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "CertVal";
    public SecureSocketOptions GetSslOptions
    {
        get => SslMode?.ToLowerInvariant() switch
        {
            "none" => SecureSocketOptions.None,
            "starttls" => SecureSocketOptions.StartTls,
            "sslonconnect" => SecureSocketOptions.SslOnConnect,
            "auto" => SecureSocketOptions.Auto,
            _ => Port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
            }
        };
    }
}