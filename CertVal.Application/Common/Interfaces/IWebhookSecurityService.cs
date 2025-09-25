namespace CertVal.Application.Common.Interfaces;

public interface IWebhookSecurityService
{
    Task<(bool IsValid, Uri? Uri, string? Error)> ValidateUrlAsync(string? url, CancellationToken cancellationToken = default);
    IDictionary<string, string> SanitizeHeaders(IDictionary<string, string>? headers);
    string SanitizeValue(string? value, int maxLength = 512);
}
