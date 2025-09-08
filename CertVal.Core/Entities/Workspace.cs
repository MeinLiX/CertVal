namespace CertVal.Core.Entities;

public class Workspace
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }

    public int MaxCertificates { get; private set; } = 1000;
    public bool IsPublic { get; private set; } = false;
    public bool AllowMemberInvites { get; private set; } = true;

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

        return new Workspace
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnerId = ownerId
        };
    }

    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(int maxCertificates, bool isPublic, bool allowMemberInvites)
    {
        if (maxCertificates <= 0)
            throw new ArgumentException("Max certificates must be positive", nameof(maxCertificates));

        MaxCertificates = maxCertificates;
        IsPublic = isPublic;
        AllowMemberInvites = allowMemberInvites;
        UpdatedAt = DateTime.UtcNow;
    }
}
