using CertVal.Application.Common.Tools;
using CertVal.Application.DTOs;
using Xunit;

namespace CertVal.Application.Tests;

public class TlsGradeCalculatorTests
{
    private static SslCertInfoDto Leaf(
        bool expired = false,
        int daysRemaining = 200,
        string keyAlg = "EC",
        int keyBits = 256,
        string sig = "sha256RSA")
        => new()
        {
            IsExpired = expired,
            DaysRemaining = daysRemaining,
            PublicKeyAlgorithm = keyAlg,
            PublicKeyBits = keyBits,
            SignatureAlgorithm = sig
        };

    private static SslCheckResultDto Result(
        string protocol = "Tls13",
        bool hostnameMatches = true,
        bool chainTrusted = true,
        SslCertInfoDto? leaf = null)
        => new()
        {
            Host = "example.com",
            Port = 443,
            Reachable = true,
            NegotiatedProtocol = protocol,
            HostnameMatches = hostnameMatches,
            ChainTrusted = chainTrusted,
            Leaf = leaf ?? Leaf()
        };

    [Fact]
    public void Unreachable_GradesF()
    {
        var (grade, findings) = TlsGradeCalculator.Evaluate(new SslCheckResultDto { Reachable = false, Error = "timeout" });
        Assert.Equal("F", grade);
        Assert.Contains(findings, f => f.Severity == TlsGradeCalculator.SeverityBlocking);
    }

    [Fact]
    public void ModernEcTls13Trusted_GradesAPlus()
    {
        var (grade, _) = TlsGradeCalculator.Evaluate(Result());
        Assert.Equal("A+", grade);
    }

    [Fact]
    public void StrongRsaTls12_GradesA_NotAPlus()
    {
        var (grade, _) = TlsGradeCalculator.Evaluate(Result("Tls12", leaf: Leaf(keyAlg: "RSA", keyBits: 4096)));
        Assert.Equal("A", grade);
    }

    [Fact]
    public void HostnameMismatch_GradesF()
    {
        var (grade, findings) = TlsGradeCalculator.Evaluate(Result(hostnameMatches: false));
        Assert.Equal("F", grade);
        Assert.Contains(findings, f => f.Message.Contains("hostname", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Expired_GradesF()
    {
        var (grade, _) = TlsGradeCalculator.Evaluate(Result(leaf: Leaf(expired: true, daysRemaining: -3)));
        Assert.Equal("F", grade);
    }

    [Fact]
    public void WeakRsaKey_GradesF()
    {
        var (grade, findings) = TlsGradeCalculator.Evaluate(Result(leaf: Leaf(keyAlg: "RSA", keyBits: 1024)));
        Assert.Equal("F", grade);
        Assert.Contains(findings, f => f.Message.Contains("Weak RSA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ObsoleteProtocol_DowngradesAndWarns()
    {
        var (grade, findings) = TlsGradeCalculator.Evaluate(Result("Tls11", leaf: Leaf(keyAlg: "RSA", keyBits: 2048)));
        Assert.NotEqual("A", grade);
        Assert.NotEqual("A+", grade);
        Assert.Contains(findings, f => f.Severity == TlsGradeCalculator.SeverityWarning);
    }

    [Fact]
    public void Sha1Signature_AddsWarning()
    {
        var (_, findings) = TlsGradeCalculator.Evaluate(Result(leaf: Leaf(sig: "sha1RSA")));
        Assert.Contains(findings, f => f.Message.Contains("SHA-1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UntrustedChain_AddsWarning_AndNotAPlus()
    {
        var (grade, findings) = TlsGradeCalculator.Evaluate(Result(chainTrusted: false));
        Assert.NotEqual("A+", grade);
        Assert.Contains(findings, f => f.Message.Contains("not trusted", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExpiringSoon_AddsWarning()
    {
        var (_, findings) = TlsGradeCalculator.Evaluate(Result(leaf: Leaf(daysRemaining: 7)));
        Assert.Contains(findings, f => f.Message.Contains("expires very soon", StringComparison.OrdinalIgnoreCase));
    }
}
