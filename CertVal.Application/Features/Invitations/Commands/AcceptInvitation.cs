using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Invitations.Commands;

public record AcceptInvitationCommand : IRequest<Result>
{
    public string Token { get; init; } = string.Empty;
}

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AcceptInvitationCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var membership = await _unitOfWork.WorkspaceMembers.GetByInvitationTokenAsync(request.Token, cancellationToken);

        if (membership == null || (membership.InvitationTokenExpiresAt.HasValue && membership.InvitationTokenExpiresAt < DateTime.UtcNow))
        {
            return Result.Failure("Invitation is invalid or has expired.");
        }

        if (_currentUser.UserId.HasValue && membership.UserId != _currentUser.UserId)
        {
            return Result.Failure("Access denied - this invitation is not for the current user");
        }

        membership.AcceptInvitation();
        await _unitOfWork.WorkspaceMembers.UpdateAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}