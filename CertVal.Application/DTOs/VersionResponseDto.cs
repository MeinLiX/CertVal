namespace CertVal.Application.DTOs;

public record VersionResponseDto
{
    public string Version { get; init; } = string.Empty;
    public DateTime BuildDate { get; init; }
    public string CommitHash { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
}