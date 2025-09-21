namespace CertVal.Application.DTOs;

public record ApiInfoResponseDto
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