using CertVal.Application.DTOs;

namespace CertVal.Application.Common.Interfaces;

/// <summary>
/// Performs a live TLS handshake against a remote host and reports the served
/// certificate chain. Implementations must be called only after the target host
/// has been validated against SSRF (no private / loopback addresses).
/// </summary>
public interface ISslInspectionService
{
    Task<SslCheckResultDto> InspectAsync(string host, int port, CancellationToken cancellationToken = default);
}
