using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Workspaces.Queries;

public record GetWorkspaceByIdQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceDto>>;

public class GetWorkspaceByIdQueryValidator : AbstractValidator<GetWorkspaceByIdQuery>
{
    public GetWorkspaceByIdQueryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");
    }
}

public class GetWorkspaceByIdQueryHandler : IRequestHandler<GetWorkspaceByIdQuery, Result<WorkspaceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetWorkspaceByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(GetWorkspaceByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<WorkspaceDto>("Workspace not found");

        if (!await CanAccessWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<WorkspaceDto>("Access denied to this workspace");

        var dto = workspace.Adapt<WorkspaceDto>();
        dto = dto with
        {
            CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
            MemberCount = workspace.Members.Count + 1 // +1 for owner
        };

        return Result.Success(dto);
    }

    private async Task<bool> CanAccessWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;
        return await _unitOfWork.Workspaces.CanUserViewAsync(workspaceId, _currentUser.UserId.Value, cancellationToken);
    }
}