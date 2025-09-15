using CertVal.Core.Events;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class EmailNotificationEventHandlers :
    IDomainEventHandler<UserRegisteredEvent>,
    IDomainEventHandler<WorkspaceMemberInvitedEvent>,
    IDomainEventHandler<CertificateExpiringEvent>,
    IDomainEventHandler<CertificateExpiredEvent>
{
    private readonly IEmailNotificationPublisher _emailPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailNotificationEventHandlers> _logger;

    public EmailNotificationEventHandlers(
        IEmailNotificationPublisher emailPublisher,
        IUnitOfWork unitOfWork,
        ILogger<EmailNotificationEventHandlers> logger)
    {
        _emailPublisher = emailPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for UserRegisteredEvent: {UserId}", domainEvent.UserId);
                return;
            }

            if (string.IsNullOrEmpty(user.EmailConfirmationToken))
            {
                _logger.LogWarning("User {UserId} has no email confirmation token", domainEvent.UserId);
                return;
            }

            await _emailPublisher.PublishUserRegisteredAsync(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.EmailConfirmationToken,
                cancellationToken);

            _logger.LogInformation("Published user registration email for user {UserId} ({Email})",
                domainEvent.UserId, domainEvent.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle UserRegisteredEvent for user {UserId}", domainEvent.UserId);
        }
    }

    public async Task HandleAsync(WorkspaceMemberInvitedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(domainEvent.WorkspaceId, cancellationToken);
            var invitedUser = await _unitOfWork.Users.GetByIdAsync(domainEvent.UserId, cancellationToken);
            var inviterUser = await _unitOfWork.Users.GetByIdAsync(domainEvent.InvitedByUserId, cancellationToken);

            if (workspace == null || invitedUser == null || inviterUser == null)
            {
                _logger.LogWarning("Missing entities for WorkspaceMemberInvitedEvent: Workspace={WorkspaceExists}, InvitedUser={InvitedUserExists}, InviterUser={InviterUserExists}",
                    workspace != null, invitedUser != null, inviterUser != null);
                return;
            }

            var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(
                domainEvent.WorkspaceId, domainEvent.UserId, cancellationToken);

            if (membership == null)
            {
                _logger.LogWarning("Membership not found for WorkspaceMemberInvitedEvent: WorkspaceId={WorkspaceId}, UserId={UserId}",
                    domainEvent.WorkspaceId, domainEvent.UserId);
                return;
            }

            var invitationToken = Guid.NewGuid().ToString();

            await _emailPublisher.PublishWorkspaceInvitationAsync(
                workspace.Id,
                invitedUser.FullName,
                inviterUser.FullName,
                workspace.Name,
                invitationToken,
                membership.Role.ToString(),
                invitedUser.Email,
                cancellationToken);

            _logger.LogInformation("Published workspace invitation email for user {UserId} to workspace {WorkspaceId}",
                domainEvent.UserId, domainEvent.WorkspaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle WorkspaceMemberInvitedEvent for workspace {WorkspaceId}, user {UserId}",
                domainEvent.WorkspaceId, domainEvent.UserId);
        }
    }

    public async Task HandleAsync(CertificateExpiringEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendCertificateNotificationAsync(domainEvent.CertificateId, domainEvent.WorkspaceId,
                domainEvent.Subject, domainEvent.ExpiryDate, domainEvent.DaysUntilExpiry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CertificateExpiringEvent for certificate {CertificateId}",
                domainEvent.CertificateId);
        }
    }

    public async Task HandleAsync(CertificateExpiredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendCertificateNotificationAsync(domainEvent.CertificateId, domainEvent.WorkspaceId,
                domainEvent.Subject, domainEvent.ExpiryDate, 0, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CertificateExpiredEvent for certificate {CertificateId}",
                domainEvent.CertificateId);
        }
    }

    private async Task SendCertificateNotificationAsync(Guid certificateId, Guid workspaceId, string subject,
        DateTime expiryDate, int daysUntilExpiry, CancellationToken cancellationToken)
    {
        var certificate = await _unitOfWork.Certificates.GetByIdAsync(certificateId, cancellationToken);
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);

        if (certificate == null || workspace == null)
        {
            _logger.LogWarning("Missing entities for certificate notification: Certificate={CertificateExists}, Workspace={WorkspaceExists}",
                certificate != null, workspace != null);
            return;
        }

        // Send notification to workspace owner
        await _emailPublisher.PublishCertificateExpiringAsync(
            workspace.Owner.Email,
            workspace.Name,
            certificate.Subject,
            certificate.Issuer,
            expiryDate,
            daysUntilExpiry,
            cancellationToken);

        // Send to all workspace members with notification permissions
        var members = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(workspaceId, cancellationToken);
        var membersWithNotifications = members.Where(m =>
            m.Status == Core.Enums.WorkspaceMemberStatus.Active &&
            m.User.EmailNotificationsEnabled);

        foreach (var member in membersWithNotifications)
        {
            await _emailPublisher.PublishCertificateExpiringAsync(
                member.User.Email,
                workspace.Name,
                certificate.Subject,
                certificate.Issuer,
                expiryDate,
                daysUntilExpiry,
                cancellationToken);
        }

        _logger.LogInformation("Published certificate notification emails for certificate {CertificateId} in workspace {WorkspaceId}",
            certificateId, workspaceId);
    }
}