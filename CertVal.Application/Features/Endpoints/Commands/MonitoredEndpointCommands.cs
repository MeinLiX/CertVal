using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Endpoints.Commands;

public record CreateMonitoredEndpointCommand(Guid WorkspaceId, string Host, int Port, int CheckIntervalMinutes)
    : IRequest<Result<MonitoredEndpointDto>>;

public class CreateMonitoredEndpointCommandValidator : AbstractValidator<CreateMonitoredEndpointCommand>
{
    public CreateMonitoredEndpointCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Host).NotEmpty().MaximumLength(253);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.CheckIntervalMinutes).InclusiveBetween(5, 10080);
    }
}

public class CreateMonitoredEndpointCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<CreateMonitoredEndpointCommand, Result<MonitoredEndpointDto>>
{
    public const int MaxEndpointsPerWorkspace = 100;

    public async Task<Result<MonitoredEndpointDto>> Handle(CreateMonitoredEndpointCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<MonitoredEndpointDto>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<MonitoredEndpointDto>("Access denied to this workspace");

        var existing = (await unitOfWork.MonitoredEndpoints.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken)).ToList();

        if (existing.Count >= MaxEndpointsPerWorkspace)
            return Result.Failure<MonitoredEndpointDto>($"Endpoint limit reached (max {MaxEndpointsPerWorkspace}).");

        var normalizedHost = request.Host.Trim().ToLowerInvariant();
        if (existing.Any(e => e.Host == normalizedHost && e.Port == request.Port))
            return Result.Failure<MonitoredEndpointDto>($"'{normalizedHost}:{request.Port}' is already monitored in this workspace.");

        MonitoredEndpoint endpoint;
        try
        {
            endpoint = MonitoredEndpoint.Create(request.WorkspaceId, request.Host, request.Port, request.CheckIntervalMinutes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MonitoredEndpointDto>(ex.Message);
        }

        await unitOfWork.MonitoredEndpoints.AddAsync(endpoint, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(endpoint.Adapt<MonitoredEndpointDto>());
    }
}

public record UpdateMonitoredEndpointCommand(Guid WorkspaceId, Guid EndpointId, string Host, int Port, bool IsEnabled, int CheckIntervalMinutes)
    : IRequest<Result<MonitoredEndpointDto>>;

public class UpdateMonitoredEndpointCommandValidator : AbstractValidator<UpdateMonitoredEndpointCommand>
{
    public UpdateMonitoredEndpointCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.EndpointId).NotEmpty();
        RuleFor(x => x.Host).NotEmpty().MaximumLength(253);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.CheckIntervalMinutes).InclusiveBetween(5, 10080);
    }
}

public class UpdateMonitoredEndpointCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<UpdateMonitoredEndpointCommand, Result<MonitoredEndpointDto>>
{
    public async Task<Result<MonitoredEndpointDto>> Handle(UpdateMonitoredEndpointCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<MonitoredEndpointDto>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<MonitoredEndpointDto>("Access denied to this workspace");

        var endpoint = await unitOfWork.MonitoredEndpoints.GetByIdAsync(request.EndpointId, cancellationToken);
        if (endpoint is null || endpoint.WorkspaceId != request.WorkspaceId)
            return Result.Failure<MonitoredEndpointDto>("Endpoint not found");

        try
        {
            endpoint.UpdateSettings(request.Host, request.Port, request.IsEnabled, request.CheckIntervalMinutes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MonitoredEndpointDto>(ex.Message);
        }

        await unitOfWork.MonitoredEndpoints.UpdateAsync(endpoint, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(endpoint.Adapt<MonitoredEndpointDto>());
    }
}

public record DeleteMonitoredEndpointCommand(Guid WorkspaceId, Guid EndpointId) : IRequest<Result<Unit>>;

public class DeleteMonitoredEndpointCommandValidator : AbstractValidator<DeleteMonitoredEndpointCommand>
{
    public DeleteMonitoredEndpointCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.EndpointId).NotEmpty();
    }
}

public class DeleteMonitoredEndpointCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<DeleteMonitoredEndpointCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteMonitoredEndpointCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return Result.Failure<Unit>("User not authenticated");

        if (!await unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, currentUser.UserId.Value, cancellationToken))
            return Result.Failure<Unit>("Access denied to this workspace");

        var endpoint = await unitOfWork.MonitoredEndpoints.GetByIdAsync(request.EndpointId, cancellationToken);
        if (endpoint is null || endpoint.WorkspaceId != request.WorkspaceId)
            return Result.Failure<Unit>("Endpoint not found");

        await unitOfWork.MonitoredEndpoints.DeleteAsync(endpoint.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
