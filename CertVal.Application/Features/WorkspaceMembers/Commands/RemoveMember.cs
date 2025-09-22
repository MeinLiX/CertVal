using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record RemoveMemberCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
}

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemoveMemberCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure("Access denied - insufficient permissions to manage this workspace");

        if (workspace.OwnerId == request.UserId)
            return Result.Failure("Cannot remove workspace owner. Transfer ownership first.");

        var member = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, request.UserId, cancellationToken: cancellationToken);
        if (member == null)
            return Result.Failure("User is not a member of this workspace");

        if (member.Status == WorkspaceMemberStatus.Inactive)
            return Result.Failure("User is already inactive in this workspace");

        // Physically delete the member
        //await _unitOfWork.WorkspaceMembers.DeleteAsync(member.Id, cancellationToken);
        
        member.Deactivate();
        await _unitOfWork.WorkspaceMembers.UpdateAsync(member, cancellationToken);
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