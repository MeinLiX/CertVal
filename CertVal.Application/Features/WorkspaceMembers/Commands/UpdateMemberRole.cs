using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record UpdateMemberRoleCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
    public WorkspaceRole NewRole { get; init; }
}

public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("New role must be a valid workspace role");
    }
}

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateMemberRoleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure("Access denied - insufficient permissions to manage this workspace");

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, request.UserId, cancellationToken: cancellationToken);
        if (membership == null)
            return Result.Failure("Member not found in this workspace");

        if (membership.Status != WorkspaceMemberStatus.Active)
            return Result.Failure("Cannot update role for inactive member");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace?.OwnerId == request.UserId)
            return Result.Failure("Cannot change role of workspace owner");

        membership.ChangeRole(request.NewRole);
        await _unitOfWork.WorkspaceMembers.UpdateAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
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