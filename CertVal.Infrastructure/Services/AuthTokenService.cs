using System.Security.Cryptography;
using System.Text;
using CertVal.Application.Common.Interfaces;
using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Configuration;

namespace CertVal.Infrastructure.Services;

public class AuthTokenService : IAuthTokenService
{
    private const int RefreshTokenByteLength = 32;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthTokenService(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> IssueTokensAsync(User user, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var response = await CreateTokenPairAsync(user, ipAddress, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<Result<LoginResponse>> RefreshAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure<LoginResponse>("Invalid refresh token");

        var tokenHash = HashToken(refreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
            return Result.Failure<LoginResponse>("Invalid refresh token");

        if (stored.IsRevoked)
        {
            await _unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(stored.UserId, "Refresh token reuse detected", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<LoginResponse>("Invalid refresh token");
        }

        if (stored.IsExpired)
            return Result.Failure<LoginResponse>("Refresh token expired");

        var user = stored.User;

        var newRefreshTokenValue = GenerateRefreshTokenValue();
        var newRefreshTokenHash = HashToken(newRefreshTokenValue);

        stored.Revoke(ipAddress, "Rotated", newRefreshTokenHash);

        var response = await PersistTokenPairAsync(user, newRefreshTokenValue, newRefreshTokenHash, ipAddress, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(response);
    }

    public async Task RevokeAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken);
        var stored = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null || !stored.IsActive)
            return;

        stored.Revoke(ipAddress, "Logout");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<LoginResponse> CreateTokenPairAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var refreshTokenValue = GenerateRefreshTokenValue();
        var refreshTokenHash = HashToken(refreshTokenValue);
        return await PersistTokenPairAsync(user, refreshTokenValue, refreshTokenHash, ipAddress, cancellationToken);
    }

    private async Task<LoginResponse> PersistTokenPairAsync(
        User user,
        string refreshTokenValue,
        string refreshTokenHash,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateToken(user);
        var accessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiry();
        var refreshTokenExpiresAt = GetRefreshTokenExpiry();

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, refreshTokenExpiresAt, ipAddress);
        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        return new LoginResponse
        {
            Token = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = MapToDto(user)
        };
    }

    private DateTime GetRefreshTokenExpiry()
    {
        var days = _configuration.GetSection("JwtSettings").GetValue<int?>("RefreshTokenExpiryDays") ?? 7;
        return DateTime.UtcNow.AddDays(days);
    }

    private static string GenerateRefreshTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static UserDto MapToDto(User user) => new()
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
    };
}
