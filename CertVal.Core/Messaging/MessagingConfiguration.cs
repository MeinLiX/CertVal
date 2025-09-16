namespace CertVal.Core.Messaging;

public class MessagingConfiguration
{
    public const string SectionName = "Messaging";

    public string ExchangeName { get; set; } = "certval-notifications";
    public string EmailQueueName { get; set; } = "email-notifications";
    public string EmailRoutingKey { get; set; } = "email";
    public bool DurableQueues { get; set; } = true;
    public bool PersistentMessages { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    // Connection settings
    public string ConnectionString { get; set; } = string.Empty;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConnectionRetries { get; set; } = 5;
}