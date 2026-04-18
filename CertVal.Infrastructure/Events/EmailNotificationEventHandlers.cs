using CertVal.Core.Events;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure.Events;

public class EmailNotificationEventHandlers :
    IDomainEventHandler<UserRegisteredEvent>,
    IDomainEventHandler<WorkspaceMemberInvitedEvent>,
    IDomainEventHandler<CertificateRevokedEvent>
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
                _logger.LogWarning("Missing entities for WorkspaceMemberInvitedEvent");
                return;
            }

            var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(
                domainEvent.WorkspaceId, domainEvent.UserId, false, cancellationToken);

            if (membership == null)
            {
                _logger.LogWarning("Membership not found for WorkspaceMemberInvitedEvent: WorkspaceId={WorkspaceId}, UserId={UserId}",
                    domainEvent.WorkspaceId, domainEvent.UserId);
                return;
            }

            await _emailPublisher.PublishWorkspaceInvitationAsync(
               workspace.Id,
               invitedUser.FullName,
               inviterUser.FullName,
               workspace.Name,
               domainEvent.InvitationToken,
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

    public async Task HandleAsync(CertificateRevokedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = await _unitOfWork.Certificates.GetByIdAsync(domainEvent.CertificateId, cancellationToken);
            if (certificate?.Workspace == null)
            {
                _logger.LogWarning("Certificate or workspace missing for CertificateRevokedEvent {CertificateId}", domainEvent.CertificateId);
                return;
            }

            var recipients = new List<string>();
            if (certificate.Workspace.Owner is { } owner && !string.IsNullOrWhiteSpace(owner.Email))
                recipients.Add(owner.Email);

            var members = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(domainEvent.WorkspaceId, cancellationToken);
            foreach (var member in members)
            {
                if (member.Status != Core.Enums.WorkspaceMemberStatus.Active) continue;
                if (member.User?.Email is { } email && !string.IsNullOrWhiteSpace(email))
                    recipients.Add(email);
            }

            if (recipients.Count == 0)
            {
                _logger.LogDebug("No recipients found for revocation notification on certificate {CertificateId}", domainEvent.CertificateId);
                return;
            }

            await _emailPublisher.PublishCertificateRevokedAsync(
                recipients,
                certificate.Workspace.Name,
                certificate.Subject,
                certificate.Issuer,
                certificate.SerialNumber,
                domainEvent.RevokedAt,
                domainEvent.Reason,
                cancellationToken);

            _logger.LogInformation("Published certificate revocation email for certificate {CertificateId} to {RecipientCount} recipient(s)",
                domainEvent.CertificateId, recipients.Distinct().Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CertificateRevokedEvent for certificate {CertificateId}", domainEvent.CertificateId);
        }
    }
}
