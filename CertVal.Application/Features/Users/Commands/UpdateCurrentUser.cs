using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Repositories;
using FluentValidation;
using Mapster;
using MediatR;

namespace CertVal.Application.Features.Users.Commands;

public record UpdateCurrentUserCommand(UpdateUserRequest Dto) : IRequest<Result<UserDto>>;

public class UpdateCurrentUserCommandValidator : AbstractValidator<UpdateCurrentUserCommand>
{
    public UpdateCurrentUserCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Dto.TimeZone)
            .MaximumLength(50).WithMessage("Time zone must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Dto.TimeZone));

        RuleFor(x => x.Dto.Language)
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.Dto.Language));
    }
}

public class UpdateCurrentUserCommandHandler : IRequestHandler<UpdateCurrentUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateCurrentUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(UpdateCurrentUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<UserDto>("User not authenticated");

        var user = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>("User not found");

        user.UpdateProfile(request.Dto.FirstName, request.Dto.LastName, request.Dto.TimeZone, request.Dto.Language);
        user.UpdateNotificationPreferences(request.Dto.EmailNotificationsEnabled);

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Adapt<UserDto>());
    }
}