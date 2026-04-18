namespace CertVal.Core.Events;

public abstract record DomainEvent(Guid Id, DateTime OccurredAt)
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}

// User events
public record UserRegisteredEvent(Guid UserId, string Email, string FullName) : DomainEvent;
public record UserEmailConfirmedEvent(Guid UserId, string Email) : DomainEvent;
public record UserPasswordChangedEvent(Guid UserId) : DomainEvent;

// Workspace events
public record WorkspaceCreatedEvent(Guid WorkspaceId, Guid OwnerId, string Name) : DomainEvent;
public record WorkspaceUpdatedEvent(Guid WorkspaceId, string Name) : DomainEvent;
public record WorkspaceOwnershipTransferredEvent(Guid WorkspaceId, Guid OldOwnerId, Guid NewOwnerId) : DomainEvent;
public record WorkspaceMemberInvitedEvent(Guid WorkspaceId, Guid UserId, Guid InvitedByUserId, string InvitationToken) : DomainEvent;
public record WorkspaceMemberJoinedEvent(Guid WorkspaceId, Guid UserId) : DomainEvent;
public record WorkspaceMemberRemovedEvent(Guid WorkspaceId, Guid UserId) : DomainEvent;

// Certificate events
public record CertificateUploadedEvent(Guid CertificateId, Guid WorkspaceId, string Subject, DateTime ExpiryDate) : DomainEvent;
public record CertificateExpiringEvent(Guid CertificateId, Guid WorkspaceId, string Subject, DateTime ExpiryDate, int DaysUntilExpiry) : DomainEvent;
public record CertificateExpiredEvent(Guid CertificateId, Guid WorkspaceId, string Subject, DateTime ExpiryDate) : DomainEvent;
public record CertificateBundleProcessedEvent(Guid ParentCertificateId, Guid WorkspaceId, int CertificateCount) : DomainEvent;
public record CertificateRevokedEvent(Guid CertificateId, Guid WorkspaceId, string Subject, string Issuer, DateTime RevokedAt, string? Reason) : DomainEvent;

// Notification events
public record NotificationRuleCreatedEvent(Guid RuleId, Guid WorkspaceId, string Name, int DaysBeforeExpiry) : DomainEvent;
public record NotificationSentEvent(Guid NotificationId, Guid CertificateId, string Recipient, string Channel) : DomainEvent;
public record NotificationFailedEvent(Guid NotificationId, Guid CertificateId, string Recipient, string Error) : DomainEvent;

// API events
public record ApiTokenCreatedEvent(Guid TokenId, Guid UserId, string Name, string Scope) : DomainEvent;
public record ApiTokenUsedEvent(Guid TokenId, Guid UserId, string? IpAddress) : DomainEvent;
public record ApiTokenRevokedEvent(Guid TokenId, Guid UserId) : DomainEvent;