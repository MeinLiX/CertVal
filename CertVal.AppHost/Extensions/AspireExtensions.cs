using Microsoft.Extensions.Configuration;

namespace CertVal.AppHost.Extensions;

public static class AspireExtensions
{
    public static IResourceBuilder<T> WithMessagingConfig<T>(
        this IResourceBuilder<T> builder,
        IConfiguration configuration) where T : IResourceWithEnvironment
    {
        var messagingConfig = configuration.GetSection("Messaging");

        return builder
            .WithEnvironment("Messaging__ExchangeName", messagingConfig["ExchangeName"] ?? throw new InvalidOperationException("Missing configuration: Messaging:ExchangeName"))
            .WithEnvironment("Messaging__EmailQueueName", messagingConfig["EmailQueueName"] ?? throw new InvalidOperationException("Missing configuration: Messaging:EmailQueueName"))
            .WithEnvironment("Messaging__EmailRoutingKey", messagingConfig["EmailRoutingKey"] ?? throw new InvalidOperationException("Missing configuration: Messaging:EmailRoutingKey"))
            .WithEnvironment("Messaging__DurableQueues", messagingConfig["DurableQueues"] ?? throw new InvalidOperationException("Missing configuration: Messaging:DurableQueues"))
            .WithEnvironment("Messaging__PersistentMessages", messagingConfig["PersistentMessages"] ?? throw new InvalidOperationException("Missing configuration: Messaging:PersistentMessages"))
            .WithEnvironment("Messaging__MaxRetryAttempts", messagingConfig["MaxRetryAttempts"] ?? throw new InvalidOperationException("Missing configuration: Messaging:MaxRetryAttempts"))
            .WithEnvironment("Messaging__RetryDelay", messagingConfig["RetryDelay"] ?? throw new InvalidOperationException("Missing configuration: Messaging:RetryDelay"))
            .WithEnvironment("Messaging__ConnectionTimeout", messagingConfig["ConnectionTimeout"] ?? throw new InvalidOperationException("Missing configuration: Messaging:ConnectionTimeout"))
            .WithEnvironment("Messaging__MaxConnectionRetries", messagingConfig["MaxConnectionRetries"] ?? throw new InvalidOperationException("Missing configuration: Messaging:MaxConnectionRetries"));
    }
}
