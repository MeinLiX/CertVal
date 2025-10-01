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
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleAuthService _googleAuthService;

    public LoginWithGoogleCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IGoogleAuthService googleAuthService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(user);

        return Result.Success(new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsEmailConfirmed = user.IsEmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                Status = user.Status.ToString(),
                TimeZone = user.TimeZone,
                Language = user.Language,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled,
                CreatedAt = user.CreatedAt
            }
        });
    }
}
