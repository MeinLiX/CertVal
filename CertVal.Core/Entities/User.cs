using CertVal.Core.Enums;
using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class User : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    public bool IsEmailConfirmed { get; private set; } = false;
    public string? EmailConfirmationToken { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }

    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;
    public UserStatus Status { get; private set; } = UserStatus.Active;

    public string? TimeZone { get; private set; } = "UTC";
    public string? Language { get; private set; } = "en";
    public bool EmailNotificationsEnabled { get; private set; } = true;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Workspace> OwnedWorkspaces { get; private set; } = [];
    public virtual ICollection<WorkspaceMember> WorkspaceMemberships { get; private set; } = [];
    public virtual ICollection<ApiToken> ApiTokens { get; private set; } = [];

    private User() { } // EF Constructor

    public static User Create(string email, string passwordHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            EmailConfirmationToken = Guid.NewGuid().ToString()
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.FullName));

        return user;
    }

    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
        EmailConfirmedAt = DateTime.UtcNow;
        EmailConfirmationToken = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserEmailConfirmedEvent(Id, Email));
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string? timeZone = null, string? language = null)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        if (!string.IsNullOrWhiteSpace(timeZone)) TimeZone = timeZone;
        if (!string.IsNullOrWhiteSpace(language)) Language = language;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotificationPreferences(bool emailNotificationsEnabled)
    {
        EmailNotificationsEnabled = emailNotificationsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(UserStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}".Trim();
}