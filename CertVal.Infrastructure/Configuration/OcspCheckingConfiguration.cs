using System.ComponentModel.DataAnnotations;

namespace CertVal.Infrastructure.Configuration;


public sealed class OcspCheckingConfiguration
{
    public const string SectionName = "OcspChecking";

    /// <summary>
    /// How often the background loop wakes up to look for due certificates.
    /// </summary>
    [Range(1, 720)]
    public int CycleIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Initial delay after service start before the first cycle runs.
    /// </summary>
    [Range(0, 600)]
    public int StartupDelaySeconds { get; set; } = 45;

    /// <summary>
    /// Minimum gap between two OCSP checks for the same certificate.
    /// </summary>
    [Range(15, 1440)]
    public int MinPerCertificateIntervalMinutes { get; set; } = 360;

    /// <summary>
    /// Maximum number of certificates processed per cycle.
    /// </summary>
    [Range(1, 5000)]
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum concurrent OCSP requests issued in parallel.
    /// </summary>
    [Range(1, 64)]
    public int MaxConcurrency { get; set; } = 6;

    /// <summary>
    /// HTTP timeout for the OCSP responder request.
    /// </summary>
    [Range(2, 120)]
    public int HttpTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Pause inserted between each cycle's last item and the next cycle, to soften load.
    /// </summary>
    [Range(0, 600)]
    public int InterCycleCooldownSeconds { get; set; } = 0;
}
