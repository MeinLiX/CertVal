using CertVal.Core.Enums;

namespace CertVal.Application.Common.Interfaces;

public sealed record OcspCheckResult(
    OcspStatus Status,
    string? ResponderUrl,
    string? RevocationReason = null,
    DateTime? RevokedAt = null,
    string? Error = null);

public interface IOcspCheckService
{
    Task<OcspCheckResult> CheckAsync(byte[] certificateBytes, string originalFileName, CancellationToken cancellationToken = default);
}
