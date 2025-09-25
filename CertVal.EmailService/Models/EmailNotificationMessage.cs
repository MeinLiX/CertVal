namespace CertVal.EmailService.Models;

public record EmailTemplate
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
}

public record UserRegisteredData
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ConfirmationToken { get; init; } = string.Empty;
}

public record WorkspaceInvitationData
{
    public string InviteeName { get; init; } = string.Empty;
    public string InviterName { get; init; } = string.Empty;
    public string WorkspaceName { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public string InvitationToken { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record PasswordResetData
{
    public string FirstName { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record CertificateExpiringData
{
    public string WorkspaceName { get; init; } = string.Empty;
    public string CertificateSubject { get; init; } = string.Empty;
    public string CertificateIssuer { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
    public int DaysUntilExpiry { get; init; }
}