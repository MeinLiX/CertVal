using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Commands;

public record TransferOwnershipByEmailCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
    public string NewOwnerEmail { get; init; } = string.Empty;
}

public class TransferOwnershipByEmailCommandValidator : AbstractValidator<TransferOwnershipByEmailCommand>
{
    public TransferOwnershipByEmailCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.NewOwnerEmail)
            .NotEmpty().WithMessage("New owner email is required")
            .EmailAddress().WithMessage("New owner email must be a valid email address")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters");
    }
}

public class TransferOwnershipByEmailCommandHandler : IRequestHandler<TransferOwnershipByEmailCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public TransferOwnershipByEmailCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(TransferOwnershipByEmailCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure("Workspace not found");

        if (workspace.OwnerId != _currentUser.UserId.Value)
            return Result.Failure("Only the workspace owner can transfer ownership");

        var newOwner = await _unitOfWork.Users.GetByEmailAsync(request.NewOwnerEmail, cancellationToken);
        if (newOwner == null)
            return Result.Failure($"User with email '{request.NewOwnerEmail}' not found");

        if (workspace.OwnerId == newOwner.Id)
            return Result.Failure("Cannot transfer ownership to yourself");

        var newOwnerMembership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(request.WorkspaceId, newOwner.Id, cancellationToken);
        if (newOwnerMembership == null)
            return Result.Failure($"User '{request.NewOwnerEmail}' must be a member of the workspace before becoming owner");

        if (newOwnerMembership.Status != WorkspaceMemberStatus.Active)
            return Result.Failure($"User '{request.NewOwnerEmail}' must be an active member of the workspace");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            workspace.TransferOwnership(newOwner.Id);
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