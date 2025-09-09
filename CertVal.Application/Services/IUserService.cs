using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;

namespace CertVal.Application.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}