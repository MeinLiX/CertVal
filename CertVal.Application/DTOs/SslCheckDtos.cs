namespace CertVal.Application.DTOs;

public record SslCheckRequest
{
    public string Host { get; init; } = string.Empty;
    public int? Port { get; init; }
}

public record SslCertInfoDto
{
    public string Subject { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public DateTime NotBefore { get; init; }
    public DateTime NotAfter { get; init; }
    public int DaysRemaining { get; init; }
    public bool IsExpired { get; init; }
    public List<string> SubjectAltNames { get; init; } = [];
    public string Sha256Thumbprint { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public string PublicKeyAlgorithm { get; init; } = string.Empty;
    public int PublicKeyBits { get; init; }
}

public record TlsFindingDto(string Severity, string Message);

public record SslCheckResultDto
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool Reachable { get; init; }
    public string? Error { get; init; }
    public string? NegotiatedProtocol { get; init; }
    public bool? HostnameMatches { get; init; }
    public bool? ChainTrusted { get; init; }
    public string? Grade { get; init; }
    public List<TlsFindingDto> Findings { get; init; } = [];
    public SslCertInfoDto? Leaf { get; init; }
    public List<SslCertInfoDto> Chain { get; init; } = [];
}
