using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.WorkspaceMembers.Queries;

public record GetWorkspaceMembersQuery : IRequest<Result<IEnumerable<WorkspaceMemberDto>>>
{
    public Guid WorkspaceId { get; init; }
    public bool IncludeInactive { get; init; } = false;
}

public class GetWorkspaceMembersQueryValidator : AbstractValidator<GetWorkspaceMembersQuery>
{
    public GetWorkspaceMembersQueryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");
    }
}

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<IEnumerable<WorkspaceMemberDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetWorkspaceMembersQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<IEnumerable<WorkspaceMemberDto>>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<IEnumerable<WorkspaceMemberDto>>("Workspace not found");

        if (!await _unitOfWork.Workspaces.CanUserAccessAsync(request.WorkspaceId, _currentUser.UserId.Value, cancellationToken))
            return Result.Failure<IEnumerable<WorkspaceMemberDto>>("Access denied to this workspace");

        var members = await _unitOfWork.WorkspaceMembers.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);

        if (!request.IncludeInactive)
        {
            members = members.Where(m => m.Status == WorkspaceMemberStatus.Active || m.Status == WorkspaceMemberStatus.Invited);
        }

        var memberDtos = new List<WorkspaceMemberDto>();

        memberDtos.Add(new WorkspaceMemberDto
        {
            Id = Guid.Empty,
            WorkspaceId = workspace.Id,
            UserId = workspace.OwnerId,
            User = workspace.Owner.Adapt<UserDto>(),
            Role = "Owner",
            Status = "Active",
            InvitedAt = null,
            JoinedAt = workspace.CreatedAt,
            CreatedAt = workspace.CreatedAt
        });

        memberDtos.AddRange(members.Select(m => new WorkspaceMemberDto
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            UserId = m.UserId,
            User = m.User.Adapt<UserDto>(),
            Role = m.Role.ToString(),
            Status = m.Status.ToString(),
            InvitedAt = m.InvitedAt,
            JoinedAt = m.JoinedAt,
            CreatedAt = m.CreatedAt
        }));

        var sortedMembers = memberDtos.OrderBy(m => GetRolePriority(m.Role))
                                     .ThenBy(m => m.User.FullName)
                                     .ToList();

        return Result.Success<IEnumerable<WorkspaceMemberDto>>(sortedMembers);
    }

    private static int GetRolePriority(string role)
    {
        return role switch
        {
            "Owner" => 0,
            "Admin" => 1,
            "Editor" => 2,
            "Viewer" => 3,
            _ => 4
        };
    }
}