namespace CertVal.Core.Messaging;

public interface IEmailNotificationPublisher : IAsyncDisposable
{
    Task PublishAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default);
    Task PublishUserRegisteredAsync(Guid userId, string email, string firstName, string lastName,
        string confirmationToken, CancellationToken cancellationToken = default);
    Task PublishWorkspaceInvitationAsync(Guid workspaceId, string inviteeName, string inviterName,
        string workspaceName, string invitationToken, string role, string email,
        CancellationToken cancellationToken = default);
    Task PublishPasswordResetAsync(string email, string firstName, string resetToken, DateTime expiresAt,
        CancellationToken cancellationToken = default);
    Task PublishCertificateExpiringAsync(string email, string workspaceName, string certificateSubject,
        string certificateIssuer, DateTime expiryDate, int daysUntilExpiry,
        CancellationToken cancellationToken = default);
}