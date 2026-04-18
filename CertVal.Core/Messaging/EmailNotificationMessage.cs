using System.Text.Json.Serialization;

namespace CertVal.Core.Messaging;

public record EmailNotificationMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public EmailNotificationType Type { get; init; }
    public string ToEmail { get; init; } = string.Empty;
    public string ToName { get; init; } = string.Empty;
    public IReadOnlyCollection<string>? Recipients { get; init; }
    public bool IsAggregated => Recipients is { Count: > 0 };
    public Dictionary<string, object> Data { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int RetryCount { get; init; } = 0;
    public string? CorrelationId { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailNotificationType
{
    UserRegistered = 1,
    EmailConfirmation = 2,
    PasswordReset = 3,
    WorkspaceInvitation = 4,
    CertificateExpiring = 5,
    CertificateExpired = 6,
    CertificateExpiryDigest = 7,
    CertificateRevoked = 8
}