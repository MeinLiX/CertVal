using System.Reflection;

namespace CertVal.Core.Events;

/// <summary>
/// Extracts persistence/audit scope (owning workspace, primary aggregate and a
/// coarse aggregate type) from a heterogeneous <see cref="DomainEvent"/> using
/// its well-known Id-bearing properties. Pure and reflection-based so new events
/// are captured without changing this code, and so it can be unit-tested.
/// </summary>
public static class DomainEventScope
{
    public static (Guid? WorkspaceId, Guid? AggregateId, string AggregateType) Extract(DomainEvent domainEvent)
    {
        var type = domainEvent.GetType();
        var name = type.Name;

        var workspaceId = GetGuid(domainEvent, type, "WorkspaceId");

        if (name.StartsWith("Certificate", StringComparison.Ordinal))
            return (workspaceId, GetGuid(domainEvent, type, "CertificateId") ?? GetGuid(domainEvent, type, "ParentCertificateId"), "Certificate");

        if (name.StartsWith("Workspace", StringComparison.Ordinal))
            return (workspaceId, workspaceId, "Workspace");

        if (name.StartsWith("Notification", StringComparison.Ordinal))
            return (workspaceId,
                GetGuid(domainEvent, type, "RuleId") ?? GetGuid(domainEvent, type, "NotificationId") ?? GetGuid(domainEvent, type, "CertificateId"),
                "Notification");

        if (name.StartsWith("ApiToken", StringComparison.Ordinal))
            return (null, GetGuid(domainEvent, type, "TokenId"), "ApiToken");

        if (name.StartsWith("User", StringComparison.Ordinal))
            return (null, GetGuid(domainEvent, type, "UserId"), "User");

        return (workspaceId, null, "Unknown");
    }

    private static Guid? GetGuid(DomainEvent domainEvent, Type type, string propertyName)
    {
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop?.GetValue(domainEvent) is Guid value && value != Guid.Empty)
            return value;
        return null;
    }
}
