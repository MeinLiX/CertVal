using CertVal.Application.Common.Audit;
using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Workspaces.Queries;

public record GetWorkspaceAuditLogQuery(Guid WorkspaceId, int Take = 100) : IRequest<Result<IReadOnlyList<AuditLogEntryDto>>>;

public class GetWorkspaceAuditLogQueryValidator : AbstractValidator<GetWorkspaceAuditLogQuery>
{
    public GetWorkspaceAuditLogQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Take).InclusiveBetween(1, 500);
    }
}

public class GetWorkspaceAuditLogQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetWorkspaceAuditLogQuery, Result<IReadOnlyList<AuditLogEntryDto>>>
{
    public async Task<Result<IReadOnlyList<AuditLogEntryDto>>> Handle(GetWorkspaceAuditLogQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<IReadOnlyList<AuditLogEntryDto>>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserViewAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<IReadOnlyList<AuditLogEntryDto>>("Access denied to this workspace");

        var events = await unitOfWork.EventStore.GetEventsByWorkspaceAsync(
            request.WorkspaceId, take: request.Take, cancellationToken: cancellationToken);

        var entries = events
            .Select(e => new AuditLogEntryDto
            {
                Id = e.Id,
                EventType = e.EventType,
                Category = AuditEventDescriptions.Category(e.EventType),
                Description = AuditEventDescriptions.Describe(e.EventType),
                AggregateId = e.AggregateId,
                OccurredAt = e.OccurredAt
            })
            .ToList();

        return Result.Success<IReadOnlyList<AuditLogEntryDto>>(entries);
    }
}
