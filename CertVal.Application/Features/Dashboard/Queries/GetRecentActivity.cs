using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Dashboard.Queries;

public record GetRecentActivityQuery : IRequest<Result<IEnumerable<RecentActivityDto>>>;

public class GetRecentActivityQueryValidator : AbstractValidator<GetRecentActivityQuery>
{
    public GetRecentActivityQueryValidator()
    {
    }
}

public class GetRecentActivityQueryHandler : IRequestHandler<GetRecentActivityQuery, Result<IEnumerable<RecentActivityDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetRecentActivityQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<RecentActivityDto>>> Handle(GetRecentActivityQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<RecentActivityDto>>("User not authenticated");

        var recentEvents = await _unitOfWork.EventStore.GetEventsByUserAsync(
            _currentUser.UserId.Value.ToString(),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            cancellationToken);

        var activities = recentEvents
            .OrderByDescending(e => e.OccurredAt)
            .Take(20)
            .Select(e => new RecentActivityDto
            {
                Id = e.Id,
                EventType = e.EventType,
                Description = GetEventDescription(e.EventType, e.EventData),
                OccurredAt = e.OccurredAt,
                AggregateId = e.AggregateId
            }).ToList();

        return Result.Success<IEnumerable<RecentActivityDto>>(activities);
    }

    private static string GetEventDescription(string eventType, string eventData)
    {
        return eventType switch
        {
            "CertificateUploadedEvent" => "Certificate uploaded",
            "CertificateExpiringEvent" => "Certificate expiring soon",
            "CertificateExpiredEvent" => "Certificate expired",
            "WorkspaceCreatedEvent" => "Workspace created",
            "WorkspaceUpdatedEvent" => "Workspace updated",
            "NotificationSentEvent" => "Notification sent",
            "NotificationFailedEvent" => "Notification failed",
            "ApiTokenCreatedEvent" => "API token created",
            "ApiTokenUsedEvent" => "API token used",
            "UserRegisteredEvent" => "User registered",
            "UserEmailConfirmedEvent" => "Email confirmed",
            "WorkspaceMemberInvitedEvent" => "Member invited",
            "WorkspaceMemberJoinedEvent" => "Member joined",
            _ => eventType.Replace("Event", "")
        };
    }
}