using CertVal.Core.Enums;

namespace CertVal.Application.DTOs;

/// <summary>
/// Strongly-typed representations of the per-channel <c>ChannelConfig</c> JSON that is
/// persisted on a <see cref="Core.Entities.NotificationRule"/>. These are validated by
/// <see cref="Common.Notifications.NotificationChannelConfigValidator"/> so malformed
/// configuration is rejected at the API boundary instead of failing at delivery time.
/// </summary>
public interface INotificationChannelConfig
{
    NotificationChannelType ChannelType { get; }
}

public sealed record EmailChannelConfig(List<Guid> UserIds) : INotificationChannelConfig
{
    public NotificationChannelType ChannelType => NotificationChannelType.Email;
}

public sealed record WebhookChannelConfig(string Url, Dictionary<string, string>? Headers = null)
    : INotificationChannelConfig
{
    public NotificationChannelType ChannelType => NotificationChannelType.Webhook;
}

public sealed record SlackChannelConfig(string WebhookUrl) : INotificationChannelConfig
{
    public NotificationChannelType ChannelType => NotificationChannelType.Slack;
}

public sealed record TelegramChannelConfig(string BotToken, string ChatId) : INotificationChannelConfig
{
    public NotificationChannelType ChannelType => NotificationChannelType.Telegram;
}
