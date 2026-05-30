using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record LoginWithGoogleCommand : IRequest<Result<LoginResponse>>
{
    public string IdToken { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthTokenService _authTokenService;
    private readonly IGoogleAuthService _googleAuthService;

    public LoginWithGoogleCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthTokenService authTokenService,
        IGoogleAuthService googleAuthService)
    {
        _unitOfWork = unitOfWork;
        _authTokenService = authTokenService;
        _googleAuthService = googleAuthService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var ext = await _googleAuthService.ValidateIdTokenAsync(request.IdToken, cancellationToken);

        var email = (ext.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<LoginResponse>("Google token is missing an email");
        if (!ext.EmailVerified)
            return Result.Failure<LoginResponse>("Google account email is not verified");

        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            var firstName = ext.FirstName ?? string.Empty;
            var lastName = ext.LastName ?? string.Empty;
            var pseudoHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
            user = User.Create(email, pseudoHash, firstName, lastName);
            user.ConfirmEmail();
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
        }

        user.UpdateLastLogin();

        var response = await _authTokenService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        return Result.Success(response);
    }
}
