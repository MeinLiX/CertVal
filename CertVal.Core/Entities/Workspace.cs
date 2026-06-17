using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class Workspace : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }

    public int MaxCertificates { get; private set; } = 1000;
    public bool IsPublic { get; private set; } = false;
    public bool AllowMemberInvites { get; private set; } = true;
    public bool AutoDeleteExpiredCertificates { get; private set; } = false;
    public bool OcspMonitoringEnabled { get; private set; } = true;

    public const int ExpiredCertificateRetentionDays = 30;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User Owner { get; private set; } = null!;
    public virtual ICollection<Certificate> Certificates { get; private set; } = [];
    public virtual ICollection<NotificationRule> NotificationRules { get; private set; } = [];
    public virtual ICollection<WorkspaceMember> Members { get; private set; } = [];

    private Workspace() { } // EF Constructor

    public static Workspace Create(string name, Guid ownerId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID cannot be empty", nameof(ownerId));

        var workspace = new Workspace
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnerId = ownerId
        };

        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.OwnerId, workspace.Name));

        return workspace;
    }

    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

        var oldName = Name;
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;

        if (oldName != Name)
        {
            AddDomainEvent(new WorkspaceUpdatedEvent(Id, Name));
        }
    }

    public void UpdateSettings(int maxCertificates, bool isPublic, bool allowMemberInvites, bool autoDeleteExpiredCertificates = false, bool ocspMonitoringEnabled = true)
    {
        if (maxCertificates <= 0)
            throw new ArgumentException("Max certificates must be positive", nameof(maxCertificates));

        MaxCertificates = maxCertificates;
        IsPublic = isPublic;
        AllowMemberInvites = allowMemberInvites;
        AutoDeleteExpiredCertificates = autoDeleteExpiredCertificates;
        OcspMonitoringEnabled = ocspMonitoringEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransferOwnership(Guid newOwnerId)
    {
        if (newOwnerId == Guid.Empty)
            throw new ArgumentException("New owner ID cannot be empty", nameof(newOwnerId));

        if (newOwnerId == OwnerId)
            throw new InvalidOperationException("Cannot transfer ownership to the same user");

        var oldOwnerId = OwnerId;
        OwnerId = newOwnerId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WorkspaceOwnershipTransferredEvent(Id, oldOwnerId, newOwnerId));
    }
}