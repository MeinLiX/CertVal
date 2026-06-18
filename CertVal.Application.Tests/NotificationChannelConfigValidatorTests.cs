using CertVal.Application.Common.Notifications;
using CertVal.Core.Enums;
using System.Text.Json;
using Xunit;

namespace CertVal.Application.Tests;

public class NotificationChannelConfigValidatorTests
{
    [Fact]
    public void Email_WithValidUserIds_Succeeds_AndDeduplicates()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { userIds = new[] { id.ToString(), id.ToString() } });

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Email, json);

        Assert.True(result.IsSuccess);
        using var doc = JsonDocument.Parse(result.Value);
        Assert.Single(doc.RootElement.GetProperty("userIds").EnumerateArray());
    }

    [Fact]
    public void Email_WithNoUserIds_Fails()
    {
        var json = JsonSerializer.Serialize(new { userIds = Array.Empty<string>() });

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Email, json);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Email_WithInvalidGuid_Fails()
    {
        var json = JsonSerializer.Serialize(new { userIds = new[] { "not-a-guid" } });

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Email, json);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("https://example.com/hook", true)]
    [InlineData("http://example.com/hook", true)]
    [InlineData("ftp://example.com/hook", false)]
    [InlineData("not-a-url", false)]
    public void Webhook_UrlSchemeValidation(string url, bool expectSuccess)
    {
        var json = JsonSerializer.Serialize(new { url });

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Webhook, json);

        Assert.Equal(expectSuccess, result.IsSuccess);
    }

    [Fact]
    public void Webhook_WithStringHeaders_NormalizesConfig()
    {
        var json = JsonSerializer.Serialize(new { url = "https://example.com/hook", headers = new { Authorization = "Bearer x" } });

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Webhook, json);

        Assert.True(result.IsSuccess);
        using var doc = JsonDocument.Parse(result.Value);
        Assert.Equal("Bearer x", doc.RootElement.GetProperty("headers").GetProperty("Authorization").GetString());
    }

    [Fact]
    public void Webhook_WithNonStringHeader_Fails()
    {
        var json = """{"url":"https://example.com/hook","headers":{"X-Count":5}}""";

        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Webhook, json);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Slack_RequiresHttpsWebhookUrl()
    {
        Assert.True(NotificationChannelConfigValidator
            .Validate(NotificationChannelType.Slack, JsonSerializer.Serialize(new { webhookUrl = "https://hooks.slack.com/services/T/B/X" }))
            .IsSuccess);

        Assert.True(NotificationChannelConfigValidator
            .Validate(NotificationChannelType.Slack, JsonSerializer.Serialize(new { webhookUrl = "http://hooks.slack.com/x" }))
            .IsFailure);
    }

    [Fact]
    public void Telegram_RequiresBotTokenAndChatId()
    {
        Assert.True(NotificationChannelConfigValidator
            .Validate(NotificationChannelType.Telegram, JsonSerializer.Serialize(new { botToken = "123:abc", chatId = "456" }))
            .IsSuccess);

        Assert.True(NotificationChannelConfigValidator
            .Validate(NotificationChannelType.Telegram, JsonSerializer.Serialize(new { botToken = "123:abc" }))
            .IsFailure);
    }

    [Fact]
    public void InvalidJson_Fails()
    {
        var result = NotificationChannelConfigValidator.Validate(NotificationChannelType.Webhook, "{not json");
        Assert.True(result.IsFailure);
    }
}
