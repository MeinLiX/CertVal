using CertVal.Application.Common.Models;

namespace CertVal.Application.Services;

public interface ICertificateProcessorService
{
    Task<Result<IEnumerable<ParsedCertificateInfo>>> ProcessCertificateAsync(
        byte[] certificateData,
        string fileName,
        CancellationToken cancellationToken = default);
}
