namespace CertVal.Application.Common.Interfaces;

public interface ICertificateRevocationProcessor
{
    Task ProcessRevocationChecksAsync(CancellationToken cancellationToken = default);
}

public interface ICertificateRevocationChecker
{
    Task TriggerCheckNowAsync(CancellationToken cancellationToken = default);
}
