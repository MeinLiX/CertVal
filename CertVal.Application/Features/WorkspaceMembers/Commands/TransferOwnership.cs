using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record TransferOwnershipCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
    public Guid NewOwnerId { get; init; }
}

public class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty().WithMessage("New owner ID is required");
    }
}

public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public TransferOwnershipCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (workspace.OwnerId != _currentUser.UserId.Value)
            return Result.Failure("Only the workspace owner can transfer ownership");

        if (workspace.OwnerId == request.NewOwnerId)
            return Result.Failure("Cannot transfer ownership to the same user");

        var newOwner = await _unitOfWork.Users.GetByIdAsync(request.NewOwnerId, cancellationToken);
        if (newOwner == null)
            return Result.Failure("New owner user not found");

        var newOwnerMembership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, request.NewOwnerId, cancellationToken);
        if (newOwnerMembership == null)
            return Result.Failure("New owner must be a member of the workspace");

        if (newOwnerMembership.Status != WorkspaceMemberStatus.Active)
            return Result.Failure("New owner must be an active member of the workspace");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            workspace.TransferOwnership(request.NewOwnerId);
            await _unitOfWork.Workspaces.UpdateAsync(workspace, cancellationToken);

            newOwnerMembership.Deactivate();
            await _unitOfWork.WorkspaceMembers.UpdateAsync(newOwnerMembership, cancellationToken);

            var oldOwnerMembership = Core.Entities.WorkspaceMember.Create(
                request.WorkspaceId,
                _currentUser.UserId.Value,
                WorkspaceRole.Admin);

            await _unitOfWork.WorkspaceMembers.AddAsync(oldOwnerMembership, cancellationToken);

        }, cancellationToken);

        return Result.Success();
    }
}
