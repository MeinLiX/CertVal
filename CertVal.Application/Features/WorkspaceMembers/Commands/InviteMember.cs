using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record InviteMemberCommand : IRequest<Result<WorkspaceMemberDto>>
{
    public Guid WorkspaceId { get; init; }
    public string Email { get; init; } = string.Empty;
    public WorkspaceRole Role { get; init; }
}

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid workspace role");
    }
}

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Result<WorkspaceMemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailNotificationPublisher _emailPublisher;

    public InviteMemberCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IEmailNotificationPublisher emailPublisher)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _emailPublisher = emailPublisher;
    }

    public async Task<Result<WorkspaceMemberDto>> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceMemberDto>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<WorkspaceMemberDto>("Workspace not found");

        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<WorkspaceMemberDto>("Access denied - insufficient permissions to manage this workspace");

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result.Failure<WorkspaceMemberDto>("User with this email does not exist");

        if (workspace.OwnerId == user.Id)
            return Result.Failure<WorkspaceMemberDto>("Cannot invite workspace owner");

        var existingMembership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, user.Id, includeInactive: true, cancellationToken);
        if (existingMembership != null && existingMembership.Status != WorkspaceMemberStatus.Inactive)
            return Result.Failure<WorkspaceMemberDto>("User is already a member of this workspace");
        
        WorkspaceMember membership;
        if (existingMembership != null)
        {
            // Reactivate existing inactive membership
            existingMembership.Reactivate(request.Role, (Guid)_currentUser.UserId);
            membership = existingMembership;
            // No need to call UpdateAsync as the entity is already tracked
        }
        else
        {
            // Create new membership
            membership = WorkspaceMember.Create(
                request.WorkspaceId,
                user.Id,
                request.Role,
                _currentUser.UserId.Value
            );

            await _unitOfWork.WorkspaceMembers.AddAsync(membership, cancellationToken);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WorkspaceMemberDto
        {
            Id = membership.Id,
            UserId = membership.UserId,
            WorkspaceId = membership.WorkspaceId,
            Role = membership.Role.ToString(),
            Status = membership.Status.ToString(),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsEmailConfirmed = user.IsEmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                Status = user.Status.ToString(),
                TimeZone = user.TimeZone,
                Language = user.Language,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled,
                CreatedAt = user.CreatedAt
            },
            JoinedAt = membership.JoinedAt,
            CreatedAt = membership.CreatedAt
        };

        return Result.Success(dto);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken: cancellationToken);
        return membership?.CanManageWorkspace ?? false;
    }
}