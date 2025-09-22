using CertVal.Application.Common.Models;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using FluentValidation;
using MediatR;

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

    public ForgotPasswordCommandHandler(IUnitOfWork unitOfWork, IEmailNotificationPublisher emailPublisher)
    {
        _unitOfWork = unitOfWork;
        _emailPublisher = emailPublisher;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user != null)
        {
            var resetToken = Guid.NewGuid().ToString();
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