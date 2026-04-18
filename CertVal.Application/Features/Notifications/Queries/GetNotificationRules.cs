using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Notifications.Queries;

public record GetNotificationRulesQuery : IRequest<Result<IEnumerable<NotificationRuleDto>>>
{
    public Guid WorkspaceId { get; init; }
}

public class GetNotificationRulesQueryValidator : AbstractValidator<GetNotificationRulesQuery>
{
    public GetNotificationRulesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");
    }
}

public class GetNotificationRulesQueryHandler : IRequestHandler<GetNotificationRulesQuery, Result<IEnumerable<NotificationRuleDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationRulesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<NotificationRuleDto>>> Handle(GetNotificationRulesQuery request, CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<IEnumerable<NotificationRuleDto>>("Access denied to this workspace");

        var rules = await _unitOfWork.NotificationRules.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var ruleDtos = rules.Select(r => new NotificationRuleDto
        {
            Id = r.Id,
            WorkspaceId = r.WorkspaceId,
            Name = r.Name,
            IsEnabled = r.IsEnabled,
            DaysBeforeExpiry = r.DaysBeforeExpiry,
            Frequency = r.Frequency.ToString(),
            ChannelType = r.ChannelType.ToString(),
            ChannelConfig = r.ChannelConfig,
            RecipientAggregationMode = r.RecipientAggregationMode,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });

        return Result.Success(ruleDtos);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserViewAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}