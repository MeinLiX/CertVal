using CertVal.Core.Enums;
using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class NotificationRule : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;

    public int DaysBeforeExpiry { get; private set; }
    public NotificationFrequency Frequency { get; private set; } = NotificationFrequency.Once;

    public NotificationChannelType ChannelType { get; private set; } = NotificationChannelType.Email;
    public string ChannelConfig { get; private set; } = "{}"; // JSON configuration

    public RecipientAggregationMode RecipientAggregationMode { get; private set; } = RecipientAggregationMode.Individual;

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

        var rule = new NotificationRule
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            DaysBeforeExpiry = daysBeforeExpiry,
            ChannelType = channelType,
            ChannelConfig = channelConfig,
            Frequency = frequency
        };

        rule.AddDomainEvent(new NotificationRuleCreatedEvent(rule.Id, rule.WorkspaceId, rule.Name, rule.DaysBeforeExpiry));

        return rule;
    }

    public void SetRecipientAggregationMode(RecipientAggregationMode mode)
    {
        if (RecipientAggregationMode != mode)
        {
            RecipientAggregationMode = mode;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfig(string channelConfig)
    {
        ChannelConfig = channelConfig;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, int daysBeforeExpiry, NotificationFrequency frequency)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (daysBeforeExpiry < 0)
            throw new ArgumentException("Days before expiry cannot be negative", nameof(daysBeforeExpiry));

        Name = name.Trim();
        DaysBeforeExpiry = daysBeforeExpiry;
        Frequency = frequency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeChannel(NotificationChannelType channelType, string channelConfig)
    {
        ChannelType = channelType;
        ChannelConfig = channelConfig;
        UpdatedAt = DateTime.UtcNow;
    }
}