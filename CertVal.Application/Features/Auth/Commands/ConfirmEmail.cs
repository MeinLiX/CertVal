using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record ConfirmEmailCommand : IRequest<Result<LoginResponse>>
{
    public string Token { get; init; } = string.Empty;
}

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");
    }
}

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public ConfirmEmailCommandHandler(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResponse>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailConfirmationTokenAsync(request.Token, cancellationToken);
        if (user == null)
            return Result.Failure<LoginResponse>("Invalid or expired confirmation token");

        user.ConfirmEmail();
        user.UpdateLastLogin();
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
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