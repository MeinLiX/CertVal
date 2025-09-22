using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Workspaces.Commands;

public record CreateWorkspaceCommand(CreateWorkspaceRequest Dto) : IRequest<Result<WorkspaceDto>>;

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("Workspace name is required")
            .MaximumLength(200).WithMessage("Workspace name must not exceed 200 characters");

        RuleFor(x => x.Dto.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Dto.Description));

        RuleFor(x => x.Dto.MaxCertificates)
            .GreaterThan(0).WithMessage("Max certificates must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Max certificates must not exceed 10000");
    }
}

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<WorkspaceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkspaceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<WorkspaceDto>("User not authenticated");

        var workspace = Core.Entities.Workspace.Create(
            request.Dto.Name,
            _currentUser.UserId.Value,
            request.Dto.Description
        );

        workspace.UpdateSettings(request.Dto.MaxCertificates, request.Dto.IsPublic, request.Dto.AllowMemberInvites);

        await _unitOfWork.Workspaces.AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdWorkspace = await _unitOfWork.Workspaces.GetByIdAsync(workspace.Id, cancellationToken);
        var dto = createdWorkspace!.Adapt<WorkspaceDto>() with
        {
            CertificateCount = 0,
            MemberCount = 1
        };

        return Result.Success(dto);
    }
}