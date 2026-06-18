using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CertVal.Application.Common.Telemetry;

/// <summary>
/// Central definitions for CertVal's custom OpenTelemetry instrumentation.
/// The <see cref="SourceName"/> is registered with the tracer and meter providers
/// in the service defaults so spans and metrics flow to the configured OTLP endpoint.
/// </summary>
public static class CertValDiagnostics
{
    public const string SourceName = "CertVal";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> OcspChecks = Meter.CreateCounter<long>(
        "certval.ocsp.checks",
        unit: "{check}",
        description: "Number of OCSP revocation checks performed, tagged by result.");

    public static readonly Counter<long> OcspRevocations = Meter.CreateCounter<long>(
        "certval.ocsp.revocations",
        unit: "{certificate}",
        description: "Certificates newly found revoked via OCSP.");

    public static readonly Histogram<double> OcspCycleDuration = Meter.CreateHistogram<double>(
        "certval.ocsp.cycle.duration",
        unit: "ms",
        description: "Wall-clock duration of a single OCSP revocation cycle.");

    public static readonly Counter<long> NotificationsSent = Meter.CreateCounter<long>(
        "certval.notifications.sent",
        unit: "{notification}",
        description: "Notifications dispatched, tagged by channel and outcome.");

    public static readonly Counter<long> CertificatesUploaded = Meter.CreateCounter<long>(
        "certval.certificates.uploaded",
        unit: "{certificate}",
        description: "Certificates successfully uploaded and processed.");
}
