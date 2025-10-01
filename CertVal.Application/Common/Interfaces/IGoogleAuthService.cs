using CertVal.Application.DTOs;

namespace CertVal.Application.Common.Interfaces;

public interface IGoogleAuthService
{
    Task<ExternalUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
