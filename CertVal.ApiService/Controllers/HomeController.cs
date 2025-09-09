using Microsoft.AspNetCore.Mvc;

namespace CertVal.ApiService.Controllers;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiInfoResponse> GetApiInfo()
    {
        var response = new ApiInfoResponse
        {
            Name = "CertVal API",
            Version = "v1.0.0",
            Description = "Certificate monitoring and management API",
            Documentation = "/scalar/v1",
            Authentication = new AuthenticationInfo
            {
                JwtBearer = new JwtInfo
                {
                    Description = "JWT token authentication for web clients",
                    HeaderName = "Authorization",
                    HeaderFormat = "Bearer {token}",
                    LoginEndpoint = "/api/v1/auth/login"
                },
                ApiKey = new ApiKeyInfo
                {
                    Description = "API key authentication for programmatic access",
                    HeaderName = "X-API-Key",
                    ManagementEndpoint = "/api/v1/apitokens"
                }
            },
            Endpoints = new EndpointsInfo
            {
                Authentication = "/api/v1/auth",
                Users = "/api/v1/users",
                Workspaces = "/api/v1/workspaces",
                Certificates = "/api/v1/certificates",
                Dashboard = "/api/v1/dashboard",
                ApiTokens = "/api/v1/apitokens"
            }
        };

        return Ok(response);
    }
}

public record ApiInfoResponse
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Documentation { get; init; } = string.Empty;
    public AuthenticationInfo Authentication { get; init; } = null!;
    public EndpointsInfo Endpoints { get; init; } = null!;
}

public record AuthenticationInfo
{
    public JwtInfo JwtBearer { get; init; } = null!;
    public ApiKeyInfo ApiKey { get; init; } = null!;
}

public record JwtInfo
{
    public string Description { get; init; } = string.Empty;
    public string HeaderName { get; init; } = string.Empty;
    public string HeaderFormat { get; init; } = string.Empty;
    public string LoginEndpoint { get; init; } = string.Empty;
}

public record ApiKeyInfo
{
    public string Description { get; init; } = string.Empty;
    public string HeaderName { get; init; } = string.Empty;
    public string ManagementEndpoint { get; init; } = string.Empty;
}

public record EndpointsInfo
{
    public string Authentication { get; init; } = string.Empty;
    public string Users { get; init; } = string.Empty;
    public string Workspaces { get; init; } = string.Empty;
    public string Certificates { get; init; } = string.Empty;
    public string Dashboard { get; init; } = string.Empty;
    public string ApiTokens { get; init; } = string.Empty;
}
