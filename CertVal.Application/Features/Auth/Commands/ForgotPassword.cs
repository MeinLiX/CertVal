using CertVal.Application.Common.Constants;
using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using CertVal.Core.Utils;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CertVal.Application.Features.Auth.Commands;

public record ForgotPasswordCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationPublisher _emailPublisher;
    private readonly IRateLimitService _rateLimitService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailNotificationPublisher emailPublisher,
        IRateLimitService rateLimitService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _emailPublisher = emailPublisher;
        _rateLimitService = rateLimitService;
        _configuration = configuration;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var rateLimitMinutes = _configuration.GetValue<int>(RateLimitConstants.ConfigurationKeys.PasswordResetMinutes, 5);
        var key = $"{RateLimitConstants.CacheKeys.PasswordResetPrefix}:{request.Email.ToLowerInvariant()}";

        if (!await _rateLimitService.IsAllowedAsync(key, TimeSpan.FromMinutes(rateLimitMinutes)))
        {
            return Result.Failure("Too many requests. Please try again later.");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user != null)
        {
            var resetToken = TokenGenerator.GenerateUrlSafeToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            user.SetPasswordResetToken(resetToken, expiresAt);
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailPublisher.PublishPasswordResetAsync(
                user.Email,
                user.FirstName,
                resetToken,
                expiresAt);
        }

        return Result.Success();
    }
}