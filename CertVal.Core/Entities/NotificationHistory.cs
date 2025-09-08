using CertVal.Core.Enums;
using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class NotificationHistory : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid NotificationRuleId { get; private set; }
    public Guid CertificateId { get; private set; }

    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public NotificationChannelType ChannelType { get; private set; }
    public string Recipient { get; private set; } = string.Empty; // Email, webhook URL, etc.

    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public DateTime ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public int MaxRetries { get; private set; } = 3;

    // Metadata for tracking
    public string? ExternalId { get; private set; } // External service message ID
    public string? ResponseData { get; private set; } // JSON response from external service

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual NotificationRule NotificationRule { get; private set; } = null!;
    public virtual Certificate Certificate { get; private set; } = null!;

    private NotificationHistory() { } // EF Constructor

    public static NotificationHistory Create(
        Guid notificationRuleId,
        Guid certificateId,
        NotificationChannelType channelType,
        string recipient,
        string subject,
        string message,
        DateTime scheduledAt)
    {
        return new NotificationHistory
        {
            NotificationRuleId = notificationRuleId,
            CertificateId = certificateId,
            ChannelType = channelType,
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Message = message,
            ScheduledAt = scheduledAt
        };
    }

    public void MarkAsSent(string? externalId = null)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalId = externalId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationSentEvent(Id, CertificateId, Recipient, ChannelType.ToString()));
    }

    public void MarkAsDelivered(string? responseData = null)
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        ResponseData = responseData;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage, string? responseData = null)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        ResponseData = responseData;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationFailedEvent(Id, CertificateId, Recipient, errorMessage));
    }

    public void ScheduleRetry(DateTime nextAttemptAt)
    {
        if (RetryCount >= MaxRetries)
            throw new InvalidOperationException("Maximum retry attempts exceeded");

        Status = NotificationStatus.Pending;
        ScheduledAt = nextAttemptAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanRetry => RetryCount < MaxRetries && Status == NotificationStatus.Failed;
    public bool IsOverdue => Status == NotificationStatus.Pending && DateTime.UtcNow > ScheduledAt.AddMinutes(30);
}