using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Mapster;

namespace CertVal.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UserService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result.Failure<UserDto>("User not authenticated");

        var user = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>("User not found");

        return Result.Success(user.Adapt<UserDto>());
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>("User not found");

        return Result.Success(user.Adapt<UserDto>());
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
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

    public async Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result.Failure<UserDto>("User not found");

        if (_currentUser.UserId != userId && !IsAdminUser())
            return Result.Failure<UserDto>("Unauthorized to update this user");

        user.UpdateProfile(request.FirstName, request.LastName, request.TimeZone, request.Language);
        user.UpdateNotificationPreferences(request.EmailNotificationsEnabled);

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Adapt<UserDto>());
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        if (_currentUser.UserId != userId)
            return Result.Failure("Unauthorized to change password for this user");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect");

        var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatePassword(newHashedPassword);

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailConfirmationTokenAsync(token, cancellationToken);
        if (user == null)
            return Result.Failure("Invalid or expired confirmation token");

        user.ConfirmEmail();
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            return Result.Success();

        var resetToken = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        user.SetPasswordResetToken(resetToken, expiresAt);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO: Send email with reset token
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(token, cancellationToken);
        if (user == null)
            return Result.Failure("Invalid or expired reset token");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatePassword(hashedPassword);

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool IsAdminUser()
    {
        return _currentUser.ApiTokenScope == Core.Enums.ApiTokenScope.Admin;
    }
}