using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Entities;

namespace CertVal.Application.Common.Interfaces;

/// <summary>
/// Issues, rotates and revokes authentication token pairs.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Issues a new access/refresh token pair for the given user.
    /// </summary>
    Task<LoginResponse> IssueTokensAsync(User user, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and rotates a refresh token, returning a fresh token pair on success.
    /// Detects reuse of a revoked token and revokes the whole token chain when that happens.
    /// </summary>
    Task<Result<LoginResponse>> RefreshAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token (used on logout). Idempotent.
    /// </summary>
    Task RevokeAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);
}
