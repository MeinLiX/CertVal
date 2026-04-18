namespace CertVal.Application.Common.Interfaces;

public interface ICertificateExpiryProcessor
{
    Task ProcessExpiryAsync(CancellationToken cancellationToken = default);
}
