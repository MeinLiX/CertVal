using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;

namespace CertVal.Application.Services;

public interface ICertificateService
{
    Task<Result<PagedResult<CertificateDto>>> GetCertificatesAsync(CertificateFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<CertificateDto>> GetCertificateByIdAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<Result<CertificateDto>> UploadCertificateAsync(UploadCertificateRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CertificateDto>>> GetExpiringCertificatesAsync(int daysAhead = 30, CancellationToken cancellationToken = default);
}