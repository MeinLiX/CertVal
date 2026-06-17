using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Events;
using Xunit;

namespace CertVal.Core.Tests;

public class CertificateTests
{
    private static Certificate NewCertificate(DateTime? notAfter = null)
    {
        return Certificate.Create(
            workspaceId: Guid.NewGuid(),
            subject: "CN=example.com",
            issuer: "CN=Test CA",
            thumbprint: "AABBCC",
            notBefore: DateTime.UtcNow.AddDays(-1),
            notAfter: notAfter ?? DateTime.UtcNow.AddDays(365),
            originalFileName: "example.pem",
            filePath: "ws/example.pem",
            fileFormat: CertificateFormat.PEM,
            fileSize: 1024);
    }

    [Fact]
    public void Create_RaisesUploadedEvent_AndIsActive()
    {
        var cert = NewCertificate();

        Assert.Equal(CertificateStatus.Active, cert.Status);
        Assert.Equal(OcspStatus.NotChecked, cert.OcspStatus);
        Assert.Contains(cert.DomainEvents, e => e is CertificateUploadedEvent);
    }

    [Fact]
    public void IsExpired_TrueForPastNotAfter()
    {
        var cert = NewCertificate(notAfter: DateTime.UtcNow.AddDays(-1));
        Assert.True(cert.IsExpired);
    }

    [Fact]
    public void IsExpiringSoon_TrueWithinWindow()
    {
        var cert = NewCertificate(notAfter: DateTime.UtcNow.AddDays(10));
        Assert.True(cert.IsExpiringSoon(30));
        Assert.False(cert.IsExpiringSoon(5));
    }

    [Fact]
    public void CheckExpiry_RaisesExpiringEvent_WhenNearExpiry()
    {
        var cert = NewCertificate(notAfter: DateTime.UtcNow.AddDays(10));
        cert.ClearDomainEvents();

        cert.CheckExpiry();

        Assert.Contains(cert.DomainEvents, e => e is CertificateExpiringEvent);
    }

    [Fact]
    public void UpdateOcspStatus_ToRevoked_SetsStatus_AndRaisesEventOnce()
    {
        var cert = NewCertificate();
        cert.ClearDomainEvents();

        cert.UpdateOcspStatus(OcspStatus.Revoked, "http://ocsp.test", "keyCompromise", DateTime.UtcNow);

        Assert.Equal(OcspStatus.Revoked, cert.OcspStatus);
        Assert.Equal(CertificateStatus.Revoked, cert.Status);
        Assert.Single(cert.DomainEvents, e => e is CertificateRevokedEvent);

        // Re-applying the same status must not raise the event again.
        cert.ClearDomainEvents();
        cert.UpdateOcspStatus(OcspStatus.Revoked);
        Assert.DoesNotContain(cert.DomainEvents, e => e is CertificateRevokedEvent);
    }

    [Fact]
    public void UpdateOcspStatus_CheckFailed_StoresLastError()
    {
        var cert = NewCertificate();

        cert.UpdateOcspStatus(OcspStatus.CheckFailed, lastError: "network error");

        Assert.Equal(OcspStatus.CheckFailed, cert.OcspStatus);
        Assert.Equal("network error", cert.OcspLastError);
    }

    [Fact]
    public void ToggleSkipMonitoring_FlipsFlag()
    {
        var cert = NewCertificate();
        Assert.False(cert.IsSkipped);

        cert.ToggleSkipMonitoring(true);
        Assert.True(cert.IsSkipped);
    }
}
