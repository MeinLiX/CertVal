using CertVal.Core.Events;

namespace CertVal.Core.Entities;

/// <summary>
/// A live TLS endpoint (host:port) that a workspace monitors. A background
/// scanner periodically connects, captures the served leaf certificate and a
/// TLS grade, and records the latest result here. When the served certificate
/// changes, a <see cref="EndpointCertificateChangedEvent"/> is raised.
/// </summary>
public class MonitoredEndpoint : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; private set; }

    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; } = 443;
    public bool IsEnabled { get; private set; } = true;
    public int CheckIntervalMinutes { get; private set; } = 360;

    // Latest scan result
    public DateTime? LastCheckedAt { get; private set; }
    public bool? LastReachable { get; private set; }
    public string? LastGrade { get; private set; }
    public string? LastProtocol { get; private set; }
    public DateTime? LeafNotAfter { get; private set; }
    public string? LeafSubject { get; private set; }
    public string? LeafThumbprint { get; private set; }
    public string? LastError { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public virtual Workspace Workspace { get; private set; } = null!;

    public const int MinCheckIntervalMinutes = 5;
    public const int MaxCheckIntervalMinutes = 10080; // 7 days

    private MonitoredEndpoint() { } // EF

    public static MonitoredEndpoint Create(Guid workspaceId, string host, int port = 443, int checkIntervalMinutes = 360)
    {
        ValidateHost(host);
        ValidatePort(port);

        return new MonitoredEndpoint
        {
            WorkspaceId = workspaceId,
            Host = host.Trim().ToLowerInvariant(),
            Port = port,
            CheckIntervalMinutes = ClampInterval(checkIntervalMinutes)
        };
    }

    public void UpdateSettings(string host, int port, bool isEnabled, int checkIntervalMinutes)
    {
        ValidateHost(host);
        ValidatePort(port);

        Host = host.Trim().ToLowerInvariant();
        Port = port;
        IsEnabled = isEnabled;
        CheckIntervalMinutes = ClampInterval(checkIntervalMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>True when the endpoint is enabled and due for its next scan.</summary>
    public bool IsDue(DateTime nowUtc)
    {
        if (!IsEnabled) return false;
        if (LastCheckedAt is null) return true;
        return LastCheckedAt.Value.AddMinutes(CheckIntervalMinutes) <= nowUtc;
    }

    /// <summary>
    /// Records a scan result. Raises <see cref="EndpointCertificateChangedEvent"/>
    /// when a reachable scan observes a different leaf certificate than before.
    /// </summary>
    public void RecordResult(
        bool reachable,
        string? grade,
        string? protocol,
        DateTime? leafNotAfter,
        string? leafSubject,
        string? leafThumbprint,
        string? error)
    {
        var previousThumbprint = LeafThumbprint;

        LastCheckedAt = DateTime.UtcNow;
        LastReachable = reachable;
        LastGrade = grade;
        LastProtocol = protocol;
        LastError = reachable ? null : error;

        if (reachable)
        {
            LeafNotAfter = leafNotAfter;
            LeafSubject = leafSubject;

            var changed = leafThumbprint is not null
                && previousThumbprint is not null
                && !string.Equals(previousThumbprint, leafThumbprint, StringComparison.OrdinalIgnoreCase);

            LeafThumbprint = leafThumbprint;

            if (changed)
            {
                AddDomainEvent(new EndpointCertificateChangedEvent(
                    Id, WorkspaceId, $"{Host}:{Port}", previousThumbprint!, leafThumbprint!));
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private static int ClampInterval(int minutes)
        => Math.Clamp(minutes, MinCheckIntervalMinutes, MaxCheckIntervalMinutes);

    private static void ValidateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be empty", nameof(host));
        if (host.Length > 253)
            throw new ArgumentException("Host is too long", nameof(host));
    }

    private static void ValidatePort(int port)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));
    }
}
