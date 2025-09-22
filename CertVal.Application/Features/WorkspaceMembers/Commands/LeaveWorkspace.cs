using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record LeaveWorkspaceCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
}

public class LeaveWorkspaceCommandValidator : AbstractValidator<LeaveWorkspaceCommand>
{
    public LeaveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");
    }
}

public class LeaveWorkspaceCommandHandler : IRequestHandler<LeaveWorkspaceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public LeaveWorkspaceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (workspace.OwnerId == _currentUser.UserId.Value)
            return Result.Failure("Workspace owner cannot leave. Transfer ownership to another member first.");

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, _currentUser.UserId.Value, cancellationToken);
        if (membership == null)
            return Result.Failure("You are not a member of this workspace");

        if (membership.Status == WorkspaceMemberStatus.Inactive)
            return Result.Failure("You have already left this workspace");

        membership.Deactivate();
        await _unitOfWork.WorkspaceMembers.UpdateAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}