using CertVal.Core.Entities;

namespace CertVal.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    DateTime GetAccessTokenExpiry();
    Task<bool> ValidateTokenAsync(string token);
}