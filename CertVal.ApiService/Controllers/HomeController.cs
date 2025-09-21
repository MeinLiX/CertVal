using CertVal.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CertVal.ApiService.Controllers;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiInfoResponseDto), StatusCodes.Status200OK)]
    public ActionResult<ApiInfoResponseDto> GetApiInfo()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var response = new ApiInfoResponseDto
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
                NotificationChannels = ["Email", "Webhook", "Slack", "Telegram"],
                MaxFileSize = "10 MB",
                MaxCertificatesPerWorkspace = 1000
            }
        };

        return Ok(response);
    }

    [HttpGet("version")]
    [ProducesResponseType(typeof(VersionResponseDto), StatusCodes.Status200OK)]
    public ActionResult<VersionResponseDto> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = new FileInfo(assembly.Location).LastWriteTime;

        var response = new VersionResponseDto
        {
            Version = version?.ToString() ?? "1.0.0",
            BuildDate = buildDate,
            CommitHash = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        };

        return Ok(response);
    }
}