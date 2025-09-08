using CertVal.Core.Enums;
using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class ApiToken : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public string TokenPrefix { get; private set; } = string.Empty; // First 8 chars for display

    public ApiTokenScope Scope { get; private set; } = ApiTokenScope.ReadOnly;
    public bool IsActive { get; private set; } = true;

    public DateTime? LastUsedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? LastUsedIpAddress { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; private set; } = null!;

    private ApiToken() { } // EF Constructor

    public static ApiToken Create(
        Guid userId,
        string name,
        string tokenHash,
        string tokenPrefix,
        ApiTokenScope scope = ApiTokenScope.ReadOnly,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Token name cannot be empty", nameof(name));

        var token = new ApiToken
        {
            UserId = userId,
            Name = name.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            Scope = scope,
            ExpiresAt = expiresAt
        };

        token.AddDomainEvent(new ApiTokenCreatedEvent(token.Id, token.UserId, token.Name, token.Scope.ToString()));

        return token;
    }

    public void UpdateLastUsed(string? ipAddress = null)
    {
        LastUsedAt = DateTime.UtcNow;
        LastUsedIpAddress = ipAddress;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ApiTokenUsedEvent(Id, UserId, ipAddress));
    }

    public void Revoke()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ApiTokenRevokedEvent(Id, UserId));
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsValid => IsActive && !IsExpired;
}