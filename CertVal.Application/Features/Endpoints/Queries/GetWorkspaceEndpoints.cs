using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Endpoints.Queries;

public record GetWorkspaceEndpointsQuery(Guid WorkspaceId) : IRequest<Result<IReadOnlyList<MonitoredEndpointDto>>>;

public class GetWorkspaceEndpointsQueryValidator : AbstractValidator<GetWorkspaceEndpointsQuery>
{
    public GetWorkspaceEndpointsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}

public class GetWorkspaceEndpointsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetWorkspaceEndpointsQuery, Result<IReadOnlyList<MonitoredEndpointDto>>>
{
    public async Task<Result<IReadOnlyList<MonitoredEndpointDto>>> Handle(GetWorkspaceEndpointsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<IReadOnlyList<MonitoredEndpointDto>>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserViewAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<IReadOnlyList<MonitoredEndpointDto>>("Access denied to this workspace");

        var endpoints = await unitOfWork.MonitoredEndpoints.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var dtos = endpoints.Select(e => e.Adapt<MonitoredEndpointDto>()).ToList();

        return Result.Success<IReadOnlyList<MonitoredEndpointDto>>(dtos);
    }
}
