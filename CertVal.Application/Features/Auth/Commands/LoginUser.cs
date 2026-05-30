using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record LoginCommand : IRequest<Result<LoginResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthTokenService _authTokenService;

    public LoginCommandHandler(IUnitOfWork unitOfWork, IAuthTokenService authTokenService)
    {
        _unitOfWork = unitOfWork;
        _authTokenService = authTokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>("Invalid email or password");

        if (!user.IsEmailConfirmed)
            return Result.Failure<LoginResponse>("Email not confirmed");

        user.UpdateLastLogin();
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        var response = await _authTokenService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        return Result.Success(response);
    }
}