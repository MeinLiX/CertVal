using CertVal.Application.Common.Audit;
using Xunit;

namespace CertVal.Application.Tests;

public class AuditEventDescriptionsTests
{
    [Theory]
    [InlineData("CertificateUploadedEvent", "Certificate uploaded")]
    [InlineData("WorkspaceMemberInvitedEvent", "Member invited")]
    [InlineData("ApiTokenRevokedEvent", "API token revoked")]
    public void Describe_KnownEvents_ReturnsFriendlyText(string eventType, string expected)
    {
        Assert.Equal(expected, AuditEventDescriptions.Describe(eventType));
    }

    [Fact]
    public void Describe_UnknownEvent_StripsEventSuffix()
    {
        Assert.Equal("SomethingHappened", AuditEventDescriptions.Describe("SomethingHappenedEvent"));
    }

    [Theory]
    [InlineData("CertificateExpiredEvent", "Certificate")]
    [InlineData("WorkspaceUpdatedEvent", "Workspace")]
    [InlineData("NotificationSentEvent", "Notification")]
    [InlineData("ApiTokenUsedEvent", "ApiToken")]
    [InlineData("UserRegisteredEvent", "User")]
    [InlineData("MysteryEvent", "Other")]
    public void Category_ClassifiesByPrefix(string eventType, string expected)
    {
        Assert.Equal(expected, AuditEventDescriptions.Category(eventType));
    }
}
