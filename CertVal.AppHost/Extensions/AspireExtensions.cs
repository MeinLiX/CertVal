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
            .WithEnvironment("Messaging__ExchangeName", messagingConfig["ExchangeName"] ?? throw new Exception())
            .WithEnvironment("Messaging__EmailQueueName", messagingConfig["EmailQueueName"] ?? throw new Exception())
            .WithEnvironment("Messaging__EmailRoutingKey", messagingConfig["EmailRoutingKey"] ?? throw new Exception())
            .WithEnvironment("Messaging__DurableQueues", messagingConfig["DurableQueues"] ?? throw new Exception())
            .WithEnvironment("Messaging__PersistentMessages", messagingConfig["PersistentMessages"] ?? throw new Exception())
            .WithEnvironment("Messaging__MaxRetryAttempts", messagingConfig["MaxRetryAttempts"] ?? throw new Exception())
            .WithEnvironment("Messaging__RetryDelay", messagingConfig["RetryDelay"] ?? throw new Exception())
            .WithEnvironment("Messaging__ConnectionTimeout", messagingConfig["ConnectionTimeout"] ?? throw new Exception())
            .WithEnvironment("Messaging__MaxConnectionRetries", messagingConfig["MaxConnectionRetries"] ?? throw new Exception());
    }
}
