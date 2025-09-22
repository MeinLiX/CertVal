using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Auth.Commands;

public record RegisterUserCommand : IRequest<Result<UserDto>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? TimeZone { get; init; }
    public string? Language { get; init; }
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.TimeZone)
            .MaximumLength(50).WithMessage("Time zone must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.TimeZone));

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.Language));
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationPublisher _emailPublisher;

    public RegisterUserCommandHandler(IUnitOfWork unitOfWork, IEmailNotificationPublisher emailPublisher)
    {
        _unitOfWork = unitOfWork;
        _emailPublisher = emailPublisher;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Users.IsEmailTakenAsync(request.Email, cancellationToken: cancellationToken))
            return Result.Failure<UserDto>("Email is already registered");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = User.Create(
            request.Email,
            hashedPassword,
            request.FirstName,
            request.LastName
        );

        if (!string.IsNullOrEmpty(request.TimeZone))
            user.UpdateProfile(user.FirstName, user.LastName, request.TimeZone, request.Language);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Adapt<UserDto>());
    }
}