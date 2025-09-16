using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CertVal.ApiService.Controllers;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(ApiInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiInfoResponse> GetApiInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var response = new ApiInfoResponse
        {
            Name = "CertVal API",
            Version = $"v{version}",
            Description = "Certificate monitoring and management API",
            Documentation = "/scalar/v1",
            ServerTime = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
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
                ApiTokens = "/api/v1/apitokens",
                Search = "/api/v1/search",
                Notifications = "/api/v1/workspaces/{workspaceId}/notifications",
                WorkspaceMembers = "/api/v1/workspaces/{workspaceId}/members",
                BulkOperations = "/api/v1/bulk",
                Exports = "/api/v1/exports"
            },
            Features = new FeaturesInfo
            {
                SupportedCertificateFormats = ["CER", "CRT", "PEM", "DER", "P7B", "P7C", "PFX", "P12"],
                NotificationChannels = new[] { "Email", "Webhook", "Slack", "Telegram" },
                MaxFileSize = "10 MB",
                MaxCertificatesPerWorkspace = 1000
            }
        };

        return Ok(response);
    }


    [HttpGet("version")]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public ActionResult<VersionResponse> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = new FileInfo(assembly.Location).LastWriteTime;

        var response = new VersionResponse
        {
            Version = version?.ToString() ?? "1.0.0",
            BuildDate = buildDate,
            CommitHash = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
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
    public DateTime ServerTime { get; init; }
    public string Environment { get; init; } = string.Empty;
    public AuthenticationInfo Authentication { get; init; } = null!;
    public EndpointsInfo Endpoints { get; init; } = null!;
    public FeaturesInfo Features { get; init; } = null!;
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
    public string Search { get; init; } = string.Empty;
    public string Notifications { get; init; } = string.Empty;
    public string WorkspaceMembers { get; init; } = string.Empty;
    public string BulkOperations { get; init; } = string.Empty;
    public string Exports { get; init; } = string.Empty;
}

public record FeaturesInfo
{
    public string[] SupportedCertificateFormats { get; init; } = Array.Empty<string>();
    public string[] NotificationChannels { get; init; } = Array.Empty<string>();
    public string MaxFileSize { get; init; } = string.Empty;
    public int MaxCertificatesPerWorkspace { get; init; }
}

public record VersionResponse
{
    public string Version { get; init; } = string.Empty;
    public DateTime BuildDate { get; init; }
    public string CommitHash { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
}