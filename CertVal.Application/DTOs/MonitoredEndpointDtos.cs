using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.DTOs;

public record MonitoredEndpointDto
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool IsEnabled { get; init; }
    public int CheckIntervalMinutes { get; init; }
    public DateTime? LastCheckedAt { get; init; }
    public bool? LastReachable { get; init; }
    public string? LastGrade { get; init; }
    public string? LastProtocol { get; init; }
    public DateTime? LeafNotAfter { get; init; }
    public string? LeafSubject { get; init; }
    public string? LeafThumbprint { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateMonitoredEndpointRequest
{
    [Required]
    [MaxLength(253)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 443;

    [Range(5, 10080)]
    public int CheckIntervalMinutes { get; init; } = 360;
}

public record UpdateMonitoredEndpointRequest
{
    [Required]
    [MaxLength(253)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 443;

    public bool IsEnabled { get; init; } = true;

    [Range(5, 10080)]
    public int CheckIntervalMinutes { get; init; } = 360;
}
