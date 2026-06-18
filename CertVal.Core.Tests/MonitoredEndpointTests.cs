using CertVal.Core.Entities;
using CertVal.Core.Events;
using Xunit;

namespace CertVal.Core.Tests;

public class MonitoredEndpointTests
{
    [Fact]
    public void Create_NormalizesHost_AndClampsInterval()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "  Example.COM ", 8443, 1);

        Assert.Equal("example.com", endpoint.Host);
        Assert.Equal(8443, endpoint.Port);
        Assert.Equal(MonitoredEndpoint.MinCheckIntervalMinutes, endpoint.CheckIntervalMinutes);
        Assert.True(endpoint.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyHost_Throws(string host)
    {
        Assert.Throws<ArgumentException>(() => MonitoredEndpoint.Create(Guid.NewGuid(), host));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void Create_WithInvalidPort_Throws(int port)
    {
        Assert.Throws<ArgumentException>(() => MonitoredEndpoint.Create(Guid.NewGuid(), "example.com", port));
    }

    [Fact]
    public void IsDue_NeverChecked_AndEnabled_IsTrue()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com", 443, 60);
        Assert.True(endpoint.IsDue(DateTime.UtcNow));
    }

    [Fact]
    public void IsDue_Disabled_IsFalse()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com", 443, 60);
        endpoint.SetEnabled(false);
        Assert.False(endpoint.IsDue(DateTime.UtcNow));
    }

    [Fact]
    public void IsDue_RespectsInterval()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com", 443, 60);
        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "ABC", null);

        Assert.False(endpoint.IsDue(DateTime.UtcNow.AddMinutes(30)));
        Assert.True(endpoint.IsDue(DateTime.UtcNow.AddMinutes(61)));
    }

    [Fact]
    public void RecordResult_FirstObservation_DoesNotRaiseChange()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com");

        endpoint.RecordResult(true, "A+", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "THUMB1", null);

        Assert.Equal("A+", endpoint.LastGrade);
        Assert.Equal("THUMB1", endpoint.LeafThumbprint);
        Assert.True(endpoint.LastReachable);
        Assert.DoesNotContain(endpoint.DomainEvents, e => e is EndpointCertificateChangedEvent);
    }

    [Fact]
    public void RecordResult_DifferentThumbprint_RaisesChangeEvent()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com");
        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "THUMB1", null);
        endpoint.ClearDomainEvents();

        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(120), "CN=example.com", "THUMB2", null);

        Assert.Equal("THUMB2", endpoint.LeafThumbprint);
        Assert.Single(endpoint.DomainEvents, e => e is EndpointCertificateChangedEvent);
    }

    [Fact]
    public void RecordResult_SameThumbprint_DoesNotRaiseChange()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com");
        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "THUMB1", null);
        endpoint.ClearDomainEvents();

        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "THUMB1", null);

        Assert.DoesNotContain(endpoint.DomainEvents, e => e is EndpointCertificateChangedEvent);
    }

    [Fact]
    public void RecordResult_Unreachable_StoresError_AndKeepsPreviousLeaf()
    {
        var endpoint = MonitoredEndpoint.Create(Guid.NewGuid(), "example.com");
        endpoint.RecordResult(true, "A", "Tls13", DateTime.UtcNow.AddDays(90), "CN=example.com", "THUMB1", null);

        endpoint.RecordResult(false, "F", null, null, null, null, "timeout");

        Assert.False(endpoint.LastReachable);
        Assert.Equal("timeout", endpoint.LastError);
        Assert.Equal("THUMB1", endpoint.LeafThumbprint); // unchanged on failure
    }
}
