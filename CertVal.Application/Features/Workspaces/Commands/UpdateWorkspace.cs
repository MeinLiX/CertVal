using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Workspaces.Commands;

public record UpdateWorkspaceCommand : IRequest<Result<WorkspaceDto>>
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MaxCertificates { get; init; } = 1000;
    public bool IsPublic { get; init; } = false;
    public bool AllowMemberInvites { get; init; } = true;
}

public class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required")
            .MaximumLength(200).WithMessage("Workspace name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.MaxCertificates)
            .GreaterThan(0).WithMessage("Max certificates must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Max certificates must not exceed 10000");
    }
}

public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, Result<WorkspaceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateWorkspaceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
            return Result.Failure<WorkspaceDto>("Workspace not found");

        if (!await CanManageWorkspace(request.WorkspaceId, cancellationToken))
            return Result.Failure<WorkspaceDto>("Access denied - insufficient permissions to manage this workspace");

        workspace.Update(request.Name, request.Description);
        workspace.UpdateSettings(request.MaxCertificates, request.IsPublic, request.AllowMemberInvites);

        await _unitOfWork.Workspaces.UpdateAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedWorkspace = await _unitOfWork.Workspaces.GetByIdAsync(workspace.Id, cancellationToken);
        var dto = updatedWorkspace!.Adapt<WorkspaceDto>() with
        {
            CertificateCount = await _unitOfWork.Certificates.GetWorkspaceCertificateCountAsync(workspace.Id, cancellationToken),
            MemberCount = updatedWorkspace.Members.Count + 1 // +1 for owner
        };

        return Result.Success(dto);
    }

    private async Task<bool> CanManageWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) return false;

        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace == null) return false;

        if (workspace.OwnerId == _currentUser.UserId.Value) return true;

        var membership = await _unitOfWork.WorkspaceMembers.GetMembershipAsync(workspaceId, _currentUser.UserId.Value, cancellationToken: cancellationToken);
        return membership?.CanManageWorkspace ?? false;
    }
}