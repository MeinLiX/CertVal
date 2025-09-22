using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Queries;

public record ValidateResetTokenQuery : IRequest<Result<ValidateResetTokenResponse>>
{
    public string Token { get; init; } = string.Empty;
}

public class ValidateResetTokenQueryValidator : AbstractValidator<ValidateResetTokenQuery>
{
    public ValidateResetTokenQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}

public class ValidateResetTokenQueryHandler : IRequestHandler<ValidateResetTokenQuery, Result<ValidateResetTokenResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateResetTokenQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ValidateResetTokenResponse>> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(request.Token, cancellationToken);
        if (user == null)
            return Result.Failure<ValidateResetTokenResponse>("Invalid or expired reset token");

        return Result.Success(new ValidateResetTokenResponse
        {
            Message = "Token is valid",
            Email = user.Email,
            ExpiresAt = user.PasswordResetTokenExpiresAt
        });
    }
}