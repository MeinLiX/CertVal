using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Notifications.Commands;

public record UpdateNotificationRuleCommand : IRequest<Result<NotificationRuleDto>>
{
    public Guid WorkspaceId { get; init; }
    public Guid RuleId { get; init; }
    public string ChannelConfig { get; init; } = string.Empty;
}

public class UpdateNotificationRuleCommandValidator : AbstractValidator<UpdateNotificationRuleCommand>
{
    public UpdateNotificationRuleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.RuleId)
            .NotEmpty().WithMessage("Rule ID is required");

        RuleFor(x => x.ChannelConfig)
            .NotEmpty().WithMessage("Channel configuration is required");
    }
}

public class UpdateNotificationRuleCommandHandler : IRequestHandler<UpdateNotificationRuleCommand, Result<NotificationRuleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateNotificationRuleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationRuleDto>> Handle(UpdateNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<NotificationRuleDto>("Access denied - insufficient permissions to manage this workspace");

        var rule = await _unitOfWork.NotificationRules.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule == null || rule.WorkspaceId != request.WorkspaceId)
            return Result.Failure<NotificationRuleDto>("Notification rule not found");

        rule.UpdateConfig(request.ChannelConfig);

        await _unitOfWork.NotificationRules.UpdateAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var ruleDto = new NotificationRuleDto
        {
            Id = rule.Id,
            WorkspaceId = rule.WorkspaceId,
            Name = rule.Name,
            IsEnabled = rule.IsEnabled,
            DaysBeforeExpiry = rule.DaysBeforeExpiry,
            Frequency = rule.Frequency.ToString(),
            ChannelType = rule.ChannelType.ToString(),
            ChannelConfig = rule.ChannelConfig,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };

        return Result.Success(ruleDto);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
        return membership?.CanManageWorkspace ?? false;
    }
}