namespace CertVal.Application.Common.Audit;

/// <summary>
/// Maps stored domain-event type names to short, human-readable descriptions for
/// the workspace audit log and dashboard activity feed. Pure and shared so both
/// surfaces stay consistent.
/// </summary>
public static class AuditEventDescriptions
{
    public static string Describe(string eventType) => eventType switch
    {
        "CertificateUploadedEvent" => "Certificate uploaded",
        "CertificateExpiringEvent" => "Certificate expiring soon",
        "CertificateExpiredEvent" => "Certificate expired",
        "CertificateRevokedEvent" => "Certificate revoked",
        "CertificateBundleProcessedEvent" => "Certificate bundle processed",
        "WorkspaceCreatedEvent" => "Workspace created",
        "WorkspaceUpdatedEvent" => "Workspace updated",
        "WorkspaceOwnershipTransferredEvent" => "Workspace ownership transferred",
        "WorkspaceMemberInvitedEvent" => "Member invited",
        "WorkspaceMemberJoinedEvent" => "Member joined",
        "WorkspaceMemberRemovedEvent" => "Member removed",
        "NotificationRuleCreatedEvent" => "Notification rule created",
        "NotificationSentEvent" => "Notification sent",
        "NotificationFailedEvent" => "Notification failed",
        "ApiTokenCreatedEvent" => "API token created",
        "ApiTokenUsedEvent" => "API token used",
        "ApiTokenRevokedEvent" => "API token revoked",
        "EndpointCertificateChangedEvent" => "Monitored endpoint certificate changed",
        "UserRegisteredEvent" => "User registered",
        "UserEmailConfirmedEvent" => "Email confirmed",
        "UserPasswordChangedEvent" => "Password changed",
        _ => eventType.Replace("Event", string.Empty)
    };

    public static string Category(string eventType)
    {
        if (eventType.StartsWith("Certificate", StringComparison.Ordinal)) return "Certificate";
        if (eventType.StartsWith("Endpoint", StringComparison.Ordinal)) return "Endpoint";
        if (eventType.StartsWith("Workspace", StringComparison.Ordinal)) return "Workspace";
        if (eventType.StartsWith("Notification", StringComparison.Ordinal)) return "Notification";
        if (eventType.StartsWith("ApiToken", StringComparison.Ordinal)) return "ApiToken";
        if (eventType.StartsWith("User", StringComparison.Ordinal)) return "User";
        return "Other";
    }
}
