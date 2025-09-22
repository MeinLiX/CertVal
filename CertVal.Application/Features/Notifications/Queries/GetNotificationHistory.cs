using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Notifications.Queries;

public record GetNotificationHistoryQuery : IRequest<Result<IEnumerable<NotificationHistoryDto>>>
{
    public Guid WorkspaceId { get; init; }
    public Guid? CertificateId { get; init; }
    public int PageSize { get; init; } = 20;
    public int PageNumber { get; init; } = 1;
}

public class GetNotificationHistoryQueryValidator : AbstractValidator<GetNotificationHistoryQuery>
{
    public GetNotificationHistoryQueryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");
    }
}

public class GetNotificationHistoryQueryHandler : IRequestHandler<GetNotificationHistoryQuery, Result<IEnumerable<NotificationHistoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationHistoryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<NotificationHistoryDto>>> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await CanAccessWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<IEnumerable<NotificationHistoryDto>>("Access denied to this workspace");

        IEnumerable<NotificationHistory> history;

        if (request.CertificateId.HasValue)
        {
            var certificate = await _unitOfWork.Certificates.GetByIdAsync(request.CertificateId.Value, cancellationToken);
            if (certificate == null || certificate.WorkspaceId != request.WorkspaceId)
                return Result.Failure<IEnumerable<NotificationHistoryDto>>("Certificate not found in this workspace");

            history = await _unitOfWork.NotificationHistory.GetByCertificateAsync(request.CertificateId.Value, cancellationToken);
        }
        else
        {
            var certificates = await _unitOfWork.Certificates.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
            var certificateIds = certificates.Select(c => c.Id).ToList();

            history = new List<NotificationHistory>();
            foreach (var certId in certificateIds)
            {
                var certHistory = await _unitOfWork.NotificationHistory.GetByCertificateAsync(certId, cancellationToken);
                history = history.Concat(certHistory);
            }
        }

        var pagedHistory = history
            .OrderByDescending(h => h.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var historyDtos = pagedHistory.Select(h => new NotificationHistoryDto
        {
            Id = h.Id,
            NotificationRuleId = h.NotificationRuleId,
            CertificateId = h.CertificateId,
            Status = h.Status.ToString(),
            ChannelType = h.ChannelType.ToString(),
            Recipient = h.Recipient,
            Subject = h.Subject,
            Message = h.Message,
            ScheduledAt = h.ScheduledAt,
            SentAt = h.SentAt,
            DeliveredAt = h.DeliveredAt,
            ErrorMessage = h.ErrorMessage,
            RetryCount = h.RetryCount,
            CreatedAt = h.CreatedAt
        });

        return Result.Success(historyDtos);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserAccessAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}