using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Events;
using Xunit;

namespace CertVal.Core.Tests;

public class WorkspaceTests
{
    [Fact]
    public void Create_SetsDefaults_AndRaisesCreatedEvent()
    {
        var ownerId = Guid.NewGuid();

        var workspace = Workspace.Create("My workspace", ownerId, "desc");

        Assert.Equal("My workspace", workspace.Name);
        Assert.Equal(ownerId, workspace.OwnerId);
        Assert.True(workspace.OcspMonitoringEnabled);
        Assert.False(workspace.AutoDeleteExpiredCertificates);
        Assert.Contains(workspace.DomainEvents, e => e is WorkspaceCreatedEvent);
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Workspace.Create("  ", Guid.NewGuid()));
    }

    [Fact]
    public void UpdateSettings_CanDisableOcspMonitoring()
    {
        var workspace = Workspace.Create("ws", Guid.NewGuid());

        workspace.UpdateSettings(500, isPublic: true, allowMemberInvites: false,
            autoDeleteExpiredCertificates: true, ocspMonitoringEnabled: false);

        Assert.Equal(500, workspace.MaxCertificates);
        Assert.True(workspace.IsPublic);
        Assert.False(workspace.AllowMemberInvites);
        Assert.True(workspace.AutoDeleteExpiredCertificates);
        Assert.False(workspace.OcspMonitoringEnabled);
    }

    [Fact]
    public void UpdateSettings_DefaultsOcspMonitoringEnabledTrue()
    {
        var workspace = Workspace.Create("ws", Guid.NewGuid());

        workspace.UpdateSettings(10, false, true);

        Assert.True(workspace.OcspMonitoringEnabled);
    }

    [Fact]
    public void TransferOwnership_ToSameOwner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("ws", ownerId);

        Assert.Throws<InvalidOperationException>(() => workspace.TransferOwnership(ownerId));
    }

    [Fact]
    public void TransferOwnership_RaisesEvent_AndChangesOwner()
    {
        var workspace = Workspace.Create("ws", Guid.NewGuid());
        var newOwner = Guid.NewGuid();

        workspace.TransferOwnership(newOwner);

        Assert.Equal(newOwner, workspace.OwnerId);
        Assert.Contains(workspace.DomainEvents, e => e is WorkspaceOwnershipTransferredEvent);
    }
}
