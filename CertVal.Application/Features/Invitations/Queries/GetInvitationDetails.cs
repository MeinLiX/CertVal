using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Invitations.Queries;

public record GetInvitationDetailsQuery : IRequest<Result<InvitationDetailsDto>>
{
    public string Token { get; init; } = string.Empty;
}

public class GetInvitationDetailsQueryValidator : AbstractValidator<GetInvitationDetailsQuery>
{
    public GetInvitationDetailsQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}

public class GetInvitationDetailsQueryHandler : IRequestHandler<GetInvitationDetailsQuery, Result<InvitationDetailsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvitationDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvitationDetailsDto>> Handle(GetInvitationDetailsQuery request, CancellationToken cancellationToken)
    {
        var membership = await _unitOfWork.WorkspaceMembers.GetByInvitationTokenAsync(request.Token, cancellationToken);

        if (membership == null || (membership.InvitationTokenExpiresAt.HasValue && membership.InvitationTokenExpiresAt < DateTime.UtcNow))
        {
            return Result.Failure<InvitationDetailsDto>("Invitation is invalid or has expired.");
        }

        return Result.Success(new InvitationDetailsDto
        {
            WorkspaceId = membership.Workspace.Id,
            WorkspaceName = membership.Workspace.Name,
            InvitedUserEmail = membership.User.Email
        });
    }
}