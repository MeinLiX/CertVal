using CertVal.Core.Repositories;
using CertVal.Core.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CertVal.Infrastructure.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.HeaderName))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKeyHeader = Request.Headers[Options.HeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var tokenHash = TokenGenerator.HashApiToken(apiKeyHeader);
            var apiToken = await unitOfWork.ApiTokens.GetActiveTokenAsync(tokenHash);

            if (apiToken == null || !apiToken.IsValid)
            {
                Logger.LogWarning("Invalid API key attempted: {KeyPrefix}", apiKeyHeader.Length > 8 ? apiKeyHeader[..8] + "..." : apiKeyHeader);
                return AuthenticateResult.Fail("Invalid API key");
            }

            apiToken.UpdateLastUsed(GetClientIpAddress());
            await unitOfWork.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, apiToken.UserId.ToString()),
                new(ClaimTypes.Email, apiToken.User.Email),
                new(ClaimTypes.GivenName, apiToken.User.FirstName),
                new(ClaimTypes.Surname, apiToken.User.LastName),
                new("client_type", "api"),
                new("api_token_id", apiToken.Id.ToString()),
                new("api_scope", apiToken.Scope.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("API key authentication successful for user {UserId} with scope {Scope}",
                apiToken.UserId, apiToken.Scope);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key");
            return AuthenticateResult.Fail("Error validating API key");
        }
    }


    private string? GetClientIpAddress()
    {
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}