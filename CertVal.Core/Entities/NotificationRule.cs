using CertVal.Core.Enums;

namespace CertVal.Core.Entities;

public class NotificationRule
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;

    // Notification timing
    public int DaysBeforeExpiry { get; private set; }
    public NotificationFrequency Frequency { get; private set; } = NotificationFrequency.Once;

    // Channel configuration
    public NotificationChannelType ChannelType { get; private set; } = NotificationChannelType.Email;
    public string ChannelConfig { get; private set; } = "{}"; // JSON configuration

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Workspace Workspace { get; private set; } = null!;
    public virtual ICollection<NotificationHistory> NotificationHistory { get; private set; } = [];

    private NotificationRule() { } // EF Constructor

    public static NotificationRule Create(
        Guid workspaceId,
        string name,
        int daysBeforeExpiry,
        NotificationChannelType channelType,
        string channelConfig,
        NotificationFrequency frequency = NotificationFrequency.Once)
    {
        if (daysBeforeExpiry < 0)
            throw new ArgumentException("Days before expiry cannot be negative", nameof(daysBeforeExpiry));

        return new NotificationRule
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            DaysBeforeExpiry = daysBeforeExpiry,
            ChannelType = channelType,
            ChannelConfig = channelConfig,
            Frequency = frequency
        };
    }

    public void Toggle() => IsEnabled = !IsEnabled;

    public void UpdateConfig(string channelConfig)
    {
        ChannelConfig = channelConfig;
        UpdatedAt = DateTime.UtcNow;
    }
}
