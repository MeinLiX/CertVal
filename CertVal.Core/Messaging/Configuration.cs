
namespace CertVal.Infrastructure.Messaging.Configuration;

public class MessagingConfiguration
{
    public const string SectionName = "Messaging";

    public string ExchangeName { get; set; } = "certval-notifications";
    public string QueueName { get; set; } = "email-notifications";
    public string RoutingKey { get; set; } = "email";
    public bool DurableQueues { get; set; } = true;
    public bool PersistentMessages { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}