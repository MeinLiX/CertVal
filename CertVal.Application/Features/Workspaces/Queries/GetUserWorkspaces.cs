using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Workspaces.Queries;

public record GetUserWorkspacesQuery : IRequest<Result<PagedResult<WorkspaceDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetUserWorkspacesQueryValidator : AbstractValidator<GetUserWorkspacesQuery>
{
    public GetUserWorkspacesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");
    }
}

public class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, Result<PagedResult<WorkspaceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetUserWorkspacesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<WorkspaceDto>>> Handle(GetUserWorkspacesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<PagedResult<WorkspaceDto>>("User not authenticated");

        var workspaces = await _unitOfWork.Workspaces.GetUserWorkspacesAsync(_currentUser.UserId.Value, cancellationToken);
        var workspaceList = workspaces.ToList();

        var totalCount = workspaceList.Count;
        var pagedWorkspaces = workspaceList
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var workspaceDtos = new List<WorkspaceDto>();
        foreach (var workspace in pagedWorkspaces)
        {
            var dto = workspace.Adapt<WorkspaceDto>();
            dto = dto with
            {
                CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
                MemberCount = workspace.Members.Count + 1 // +1 for owner
            };
            workspaceDtos.Add(dto);
        }

        var pagedResult = new PagedResult<WorkspaceDto>(workspaceDtos, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(pagedResult);
    }
}