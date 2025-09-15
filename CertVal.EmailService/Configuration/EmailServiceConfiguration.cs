namespace CertVal.EmailService.Configuration;

public class EmailServiceConfiguration
{
    public const string SectionName = "EmailService";

    public SmtpSettings Smtp { get; set; } = new();
    public EmailTemplateSettings Templates { get; set; } = new();
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public string QueueName { get; set; } = "email-notifications";
    public string ExchangeName { get; set; } = "certval-notifications";
    public string RoutingKey { get; set; } = "email";
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "CertVal";
}

public class EmailTemplateSettings
{
    public string BaseUrl { get; set; } = "https://certval.halerka.dev";
    public string SupportEmail { get; set; } = "support@certval.halerka.dev";
    public string CompanyName { get; set; } = "CertVal";
}