using CertVal.Application.Common.Models;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using CertVal.Core.Utils;
using FluentValidation;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record ResendEmailConfirmationCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
}

public class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");
    }
}

public class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationPublisher _emailPublisher;

    public ResendEmailConfirmationCommandHandler(IUnitOfWork unitOfWork, IEmailNotificationPublisher emailPublisher)
    {
        _unitOfWork = unitOfWork;
        _emailPublisher = emailPublisher;
    }

    public async Task<Result> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result.Success();

        if (user.IsEmailConfirmed)
            return Result.Failure("Email is already confirmed");

        var newToken = TokenGenerator.GenerateUrlSafeToken();
        user.SetEmailConfirmationToken(newToken);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailPublisher.PublishUserRegisteredAsync(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            newToken);

        return Result.Success();
    }
}