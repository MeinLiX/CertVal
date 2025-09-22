using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.ApiTokens.Commands;

public record RevokeApiTokenCommand(Guid TokenId) : IRequest<Result>;

public class RevokeApiTokenCommandValidator : AbstractValidator<RevokeApiTokenCommand>
{
    public RevokeApiTokenCommandValidator()
    {
        RuleFor(x => x.TokenId)
            .NotEmpty().WithMessage("Token ID is required");
    }
}

public class RevokeApiTokenCommandHandler : IRequestHandler<RevokeApiTokenCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RevokeApiTokenCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeApiTokenCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure("User not authenticated");

        var token = await _unitOfWork.ApiTokens.GetByIdAsync(request.TokenId, cancellationToken);

        if (token == null)
            return Result.Failure("API token not found");

        if (token.UserId != _currentUser.UserId.Value)
            return Result.Failure("Access denied - you can only revoke your own tokens");

        if (!token.IsActive)
            return Result.Failure("Token is already revoked");

        token.Revoke();
        await _unitOfWork.ApiTokens.UpdateAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}