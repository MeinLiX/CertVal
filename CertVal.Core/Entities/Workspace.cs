namespace CertVal.Core.Entities;

public class Workspace
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? OwnerId { get; private set; } // Future auth
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Certificate> Certificates { get; private set; } = [];
    public virtual ICollection<NotificationRule> NotificationRules { get; private set; } = [];

    private Workspace() { } // EF Constructor

    public static Workspace Create(string name, string? description = null, Guid? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty", nameof(name));

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
}
