using CertVal.Application.DTOs;

namespace CertVal.Application.Common.Tools;

/// <summary>
/// Computes a coarse, SSL-Labs-style letter grade for a single negotiated TLS
/// connection plus a list of human-readable findings. This is intentionally a
/// pure function over <see cref="SslCheckResultDto"/> so it can be unit-tested
/// in isolation. It grades the <em>observed</em> handshake (protocol, key,
/// signature, validity, hostname and chain trust) — it does not enumerate every
/// protocol/cipher the server supports.
/// </summary>
public static class TlsGradeCalculator
{
    public const string SeverityInfo = "info";
    public const string SeverityWarning = "warning";
    public const string SeverityBlocking = "blocking";

    public static (string Grade, List<TlsFindingDto> Findings) Evaluate(SslCheckResultDto result)
    {
        var findings = new List<TlsFindingDto>();

        if (!result.Reachable)
        {
            findings.Add(new TlsFindingDto(SeverityBlocking, result.Error ?? "Host is not reachable."));
            return ("F", findings);
        }

        var score = 100;
        var blocking = false;

        // --- Protocol version ---
        var protocol = (result.NegotiatedProtocol ?? string.Empty).ToLowerInvariant();
        if (protocol.Contains("13"))
        {
            findings.Add(new TlsFindingDto(SeverityInfo, "Modern protocol negotiated: TLS 1.3."));
        }
        else if (protocol.Contains("12"))
        {
            findings.Add(new TlsFindingDto(SeverityInfo, "Protocol negotiated: TLS 1.2."));
        }
        else if (protocol.Contains("11") || protocol.Contains("10"))
        {
            score -= 35;
            findings.Add(new TlsFindingDto(SeverityWarning, "Obsolete protocol negotiated (TLS 1.0/1.1). Disable it and require TLS 1.2+."));
        }
        else if (protocol.Contains("ssl"))
        {
            score -= 60;
            findings.Add(new TlsFindingDto(SeverityBlocking, "Insecure SSL protocol negotiated. This must be disabled."));
            blocking = true;
        }

        // --- Hostname match ---
        if (result.HostnameMatches == false)
        {
            score -= 100;
            blocking = true;
            findings.Add(new TlsFindingDto(SeverityBlocking, "Certificate hostname does not match the requested host."));
        }

        // --- Chain trust ---
        if (result.ChainTrusted == false && result.HostnameMatches != false)
        {
            score -= 25;
            findings.Add(new TlsFindingDto(SeverityWarning, "Certificate chain is not trusted (self-signed, incomplete chain, or private CA)."));
        }

        var leaf = result.Leaf;
        var strongKey = false;

        if (leaf is not null)
        {
            // --- Validity ---
            if (leaf.IsExpired)
            {
                score -= 100;
                blocking = true;
                findings.Add(new TlsFindingDto(SeverityBlocking, "Leaf certificate has expired."));
            }
            else if (leaf.DaysRemaining <= 14)
            {
                score -= 15;
                findings.Add(new TlsFindingDto(SeverityWarning, $"Leaf certificate expires very soon ({leaf.DaysRemaining} day(s))."));
            }
            else if (leaf.DaysRemaining <= 30)
            {
                score -= 5;
                findings.Add(new TlsFindingDto(SeverityInfo, $"Leaf certificate expires in {leaf.DaysRemaining} days."));
            }

            // --- Key strength ---
            var alg = leaf.PublicKeyAlgorithm.ToUpperInvariant();
            if (alg == "RSA")
            {
                if (leaf.PublicKeyBits is > 0 and < 2048)
                {
                    score -= 50;
                    findings.Add(new TlsFindingDto(SeverityBlocking, $"Weak RSA key ({leaf.PublicKeyBits} bits). Use at least 2048 bits."));
                    blocking = true;
                }
                else if (leaf.PublicKeyBits >= 3072)
                {
                    strongKey = true;
                }
            }
            else if (alg == "EC" || alg == "ECDSA")
            {
                strongKey = true;
            }

            // --- Signature algorithm ---
            var sig = leaf.SignatureAlgorithm.ToLowerInvariant();
            if (sig.Contains("md5"))
            {
                score -= 60;
                blocking = true;
                findings.Add(new TlsFindingDto(SeverityBlocking, "Certificate uses a broken MD5 signature."));
            }
            else if (sig.Contains("sha1"))
            {
                score -= 30;
                findings.Add(new TlsFindingDto(SeverityWarning, "Certificate uses a weak SHA-1 signature."));
            }
        }

        var grade = ScoreToGrade(score, blocking);

        // A+ reserved for a flawless, modern, strong-key configuration.
        if (grade == "A" && !blocking && protocol.Contains("13") && strongKey
            && leaf is { IsExpired: false } && leaf.DaysRemaining > 30
            && result.HostnameMatches == true && result.ChainTrusted == true)
        {
            grade = "A+";
        }

        return (grade, findings);
    }

    private static string ScoreToGrade(int score, bool blocking)
    {
        if (blocking || score < 50) return "F";
        return score switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 65 => "C",
            _ => "D"
        };
    }
}
