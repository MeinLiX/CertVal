using CertVal.Application.Common.Interfaces;
using CertVal.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CertVal.Infrastructure.Services;

/// <summary>
/// Inspects the TLS certificate served by a remote host by performing a handshake
/// and capturing the presented chain. The certificate validation callback always
/// returns <c>true</c> so that even untrusted/expired chains can be reported — the
/// trust outcome is surfaced separately via <see cref="SslCheckResultDto.ChainTrusted"/>.
/// </summary>
public sealed class SslInspectionService : ISslInspectionService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly ILogger<SslInspectionService> _logger;

    public SslInspectionService(ILogger<SslInspectionService> logger)
    {
        _logger = logger;
    }

    public async Task<SslCheckResultDto> InspectAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        X509Chain? capturedChain = null;
        var policyErrors = SslPolicyErrors.None;

        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cts.Token);

            await using var network = tcp.GetStream();
            using var ssl = new SslStream(network, leaveInnerStreamOpen: false, (_, _, chain, errors) =>
            {
                if (chain is not null)
                {
                    capturedChain = new X509Chain();
                    foreach (var element in chain.ChainElements)
                        capturedChain.ChainPolicy.ExtraStore.Add(element.Certificate);
                }
                policyErrors = errors;
                return true;
            });

            var options = new SslClientAuthenticationOptions
            {
                TargetHost = host,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };

            await ssl.AuthenticateAsClientAsync(options, cts.Token);

            var chainCerts = capturedChain?.ChainPolicy.ExtraStore
                .OfType<X509Certificate2>()
                .ToList() ?? [];

            var chainDtos = chainCerts.Select(Map).ToList();

            return new SslCheckResultDto
            {
                Host = host,
                Port = port,
                Reachable = true,
                NegotiatedProtocol = ssl.SslProtocol.ToString(),
                HostnameMatches = !policyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch),
                ChainTrusted = policyErrors == SslPolicyErrors.None,
                Leaf = chainDtos.FirstOrDefault(),
                Chain = chainDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SSL inspection failed for {Host}:{Port}", host, port);
            return new SslCheckResultDto
            {
                Host = host,
                Port = port,
                Reachable = false,
                Error = ex is OperationCanceledException ? "Connection timed out." : ex.Message
            };
        }
        finally
        {
            capturedChain?.Dispose();
        }
    }

    private static SslCertInfoDto Map(X509Certificate2 cert)
    {
        var now = DateTime.UtcNow;
        var notAfter = cert.NotAfter.ToUniversalTime();

        var sans = cert.Extensions
            .OfType<X509SubjectAlternativeNameExtension>()
            .SelectMany(ext => SafeEnumerateDnsNames(ext))
            .Distinct()
            .ToList();

        return new SslCertInfoDto
        {
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            SerialNumber = cert.SerialNumber,
            NotBefore = cert.NotBefore.ToUniversalTime(),
            NotAfter = notAfter,
            DaysRemaining = (int)Math.Floor((notAfter - now).TotalDays),
            IsExpired = now > notAfter,
            SubjectAltNames = sans,
            Sha256Thumbprint = Convert.ToHexString(cert.GetCertHash(HashAlgorithmName.SHA256)),
            SignatureAlgorithm = cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value ?? string.Empty,
            PublicKey = DescribePublicKey(cert)
        };
    }

    private static IEnumerable<string> SafeEnumerateDnsNames(X509SubjectAlternativeNameExtension ext)
    {
        try
        {
            return ext.EnumerateDnsNames().ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string DescribePublicKey(X509Certificate2 cert)
    {
        using var rsa = cert.GetRSAPublicKey();
        if (rsa is not null) return $"RSA {rsa.KeySize} bits";

        using var ecdsa = cert.GetECDsaPublicKey();
        if (ecdsa is not null) return $"EC {ecdsa.KeySize} bits";

        return cert.PublicKey.Oid.FriendlyName ?? "unknown";
    }
}
