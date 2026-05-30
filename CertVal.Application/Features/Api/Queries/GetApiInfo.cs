using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using MediatR;
using System.Reflection;

namespace CertVal.Application.Features.Api.Queries;

public record GetApiInfoQuery : IRequest<Result<ApiInfoResponseDto>>;

public class GetApiInfoQueryHandler : IRequestHandler<GetApiInfoQuery, Result<ApiInfoResponseDto>>
{
    public async Task<Result<ApiInfoResponseDto>> Handle(GetApiInfoQuery request, CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var response = new ApiInfoResponseDto
        {
            Name = "CertVal API",
            Version = $"v{version}",
            Description = "Certificate monitoring and management API",
            Documentation = "/api/scalar/v1",
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

        return await Task.FromResult(Result.Success(response));
    }
}