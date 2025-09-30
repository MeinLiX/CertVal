namespace CertVal.Application.Common.Interfaces;

public interface ICertificateStorageService
{
    Task<string> StoreCertificateAsync(Guid workspaceId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default);
    
    Task<string> StoreCertificateAsync(Guid workspaceId, Guid certificateId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default);

    Task<byte[]> GetCertificateAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteCertificateAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<bool> CertificateExistsAsync(string objectKey, CancellationToken cancellationToken = default);

    Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);

    Task DeleteWorkspaceCertificatesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}