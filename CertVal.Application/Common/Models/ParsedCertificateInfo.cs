using CertVal.Core.Enums;

namespace CertVal.Application.Common.Models;

public class ParsedCertificateInfo
{
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Thumbprint { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public CertificateFormat Format { get; set; }
}