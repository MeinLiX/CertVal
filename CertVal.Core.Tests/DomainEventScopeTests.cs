using CertVal.Core.Events;
using Xunit;

namespace CertVal.Core.Tests;

public class DomainEventScopeTests
{
    [Fact]
    public void Extract_CertificateEvent_ReturnsWorkspaceAndCertificateAggregate()
    {
        var workspaceId = Guid.NewGuid();
        var certId = Guid.NewGuid();
        var evt = new CertificateUploadedEvent(certId, workspaceId, "CN=example.com", DateTime.UtcNow.AddDays(90));

        var (ws, aggregateId, aggregateType) = DomainEventScope.Extract(evt);

        Assert.Equal(workspaceId, ws);
        Assert.Equal(certId, aggregateId);
        Assert.Equal("Certificate", aggregateType);
    }

    [Fact]
    public void Extract_WorkspaceEvent_UsesWorkspaceAsAggregate()
    {
        var workspaceId = Guid.NewGuid();
        var evt = new WorkspaceCreatedEvent(workspaceId, Guid.NewGuid(), "My WS");

        var (ws, aggregateId, aggregateType) = DomainEventScope.Extract(evt);

        Assert.Equal(workspaceId, ws);
        Assert.Equal(workspaceId, aggregateId);
        Assert.Equal("Workspace", aggregateType);
    }

    [Fact]
    public void Extract_NotificationRuleEvent_ReturnsWorkspaceAndRule()
    {
        var workspaceId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var evt = new NotificationRuleCreatedEvent(ruleId, workspaceId, "Rule", 30);

        var (ws, aggregateId, aggregateType) = DomainEventScope.Extract(evt);

        Assert.Equal(workspaceId, ws);
        Assert.Equal(ruleId, aggregateId);
        Assert.Equal("Notification", aggregateType);
    }

    [Fact]
    public void Extract_UserEvent_HasNoWorkspace()
    {
        var userId = Guid.NewGuid();
        var evt = new UserRegisteredEvent(userId, "a@b.com", "Ada Lovelace");

        var (ws, aggregateId, aggregateType) = DomainEventScope.Extract(evt);

        Assert.Null(ws);
        Assert.Equal(userId, aggregateId);
        Assert.Equal("User", aggregateType);
    }

    [Fact]
    public void Extract_ApiTokenEvent_HasNoWorkspace()
    {
        var tokenId = Guid.NewGuid();
        var evt = new ApiTokenCreatedEvent(tokenId, Guid.NewGuid(), "ci", "ReadOnly");

        var (ws, aggregateId, aggregateType) = DomainEventScope.Extract(evt);

        Assert.Null(ws);
        Assert.Equal(tokenId, aggregateId);
        Assert.Equal("ApiToken", aggregateType);
    }
}

public class StoredEventFactoryTests
{
    [Fact]
    public void FromRuntimeEvent_UsesRuntimeTypeName_AndWorkspaceScope()
    {
        var workspaceId = Guid.NewGuid();
        DomainEvent evt = new CertificateExpiredEvent(Guid.NewGuid(), workspaceId, "CN=x", DateTime.UtcNow);

        var stored = Core.Entities.StoredEvent.FromRuntimeEvent(evt);

        Assert.Equal("CertificateExpiredEvent", stored.EventType);
        Assert.Equal(workspaceId, stored.WorkspaceId);
        Assert.Contains("workspaceId", stored.EventData); // camelCase serialization of the concrete type
    }
}
