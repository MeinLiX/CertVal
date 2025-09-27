namespace CertVal.Application.Common.Interfaces;

public interface ICertificateExpiryChecker
{
    Task TriggerCheckNowAsync(CancellationToken cancellationToken = default);
}
