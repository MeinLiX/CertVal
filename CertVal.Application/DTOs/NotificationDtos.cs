using CertVal.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record NotificationRuleDto
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public int DaysBeforeExpiry { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public string ChannelType { get; init; } = string.Empty;
    public string ChannelConfig { get; init; } = string.Empty;
    public RecipientAggregationMode RecipientAggregationMode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateNotificationRuleRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(0, 365)]
    public int DaysBeforeExpiry { get; init; }

    [Required]
    public NotificationChannelType ChannelType { get; init; }

    public string ChannelConfig { get; init; } = "{}";

    public List<Guid>? RecipientUserIds { get; init; }

    public NotificationFrequency Frequency { get; init; } = NotificationFrequency.Once;
    public RecipientAggregationMode RecipientAggregationMode { get; init; } = RecipientAggregationMode.Individual;
}

public record UpdateNotificationRuleRequest
{
    [Required]
    public string ChannelConfig { get; init; } = "{}";
    public RecipientAggregationMode? RecipientAggregationMode { get; init; }
}

public record NotificationHistoryDto
{
    public Guid Id { get; init; }
    public Guid NotificationRuleId { get; init; }
    public Guid CertificateId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ChannelType { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime ScheduledAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public DateTime CreatedAt { get; init; }
}