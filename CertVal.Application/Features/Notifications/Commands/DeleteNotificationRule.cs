using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Notifications.Commands;

public record DeleteNotificationRuleCommand : IRequest<Result>
{
    public Guid WorkspaceId { get; init; }
    public Guid RuleId { get; init; }
}

public class DeleteNotificationRuleCommandValidator : AbstractValidator<DeleteNotificationRuleCommand>
{
    public DeleteNotificationRuleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.RuleId)
            .NotEmpty().WithMessage("Rule ID is required");
    }
}

public class DeleteNotificationRuleCommandHandler : IRequestHandler<DeleteNotificationRuleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteNotificationRuleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure("Access denied - insufficient permissions to manage this workspace");

        var rule = await _unitOfWork.NotificationRules.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null || rule.WorkspaceId != request.WorkspaceId)
            return Result.Failure("Notification rule not found");

        await _unitOfWork.NotificationRules.DeleteAsync(request.RuleId, cancellationToken);
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