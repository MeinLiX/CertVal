using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace CertVal.Infrastructure.Authentication;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExternalUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var clientId = _configuration["GoogleAuth:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("GoogleAuth:ClientId not configured");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new ExternalUserInfo
        {
            Email = payload.Email ?? string.Empty,
            EmailVerified = payload.EmailVerified,
            FirstName = payload.GivenName,
            LastName = payload.FamilyName,
            PictureUrl = payload.Picture
        };
    }
}
