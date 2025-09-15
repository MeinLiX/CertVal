using System.Text.Json.Serialization;

namespace CertVal.EmailService.Models;

public record EmailNotificationMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public EmailNotificationType Type { get; init; }
    public string ToEmail { get; init; } = string.Empty;
    public string ToName { get; init; } = string.Empty;
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
    CertificateExpired = 6
}

public record UserRegisteredData
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ConfirmationToken { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}

public record WorkspaceInvitationData
{
    public string InviteeName { get; init; } = string.Empty;
    public string InviterName { get; init; } = string.Empty;
    public string WorkspaceName { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public string InvitationToken { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record PasswordResetData
{
    public string FirstName { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record CertificateExpiringData
{
    public string WorkspaceName { get; init; } = string.Empty;
    public string CertificateSubject { get; init; } = string.Empty;
    public string CertificateIssuer { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
    public int DaysUntilExpiry { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
}