using CertVal.Application.Common.Notifications;
using System.Text.Json;
using Xunit;

namespace CertVal.Application.Tests;

public class ChatNotificationFormatterTests
{
    [Fact]
    public void Slack_ProducesTextField_WithSubjectAndMessage()
    {
        var json = ChatNotificationFormatter.BuildSlackPayload("Cert expiring", "renew it");

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("text").GetString();
        Assert.Contains("Cert expiring", text);
        Assert.Contains("renew it", text);
    }

    [Fact]
    public void Telegram_ProducesSnakeCaseChatId_AndText()
    {
        var json = ChatNotificationFormatter.BuildTelegramPayload("12345", "Cert expiring", "renew it");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("12345", doc.RootElement.GetProperty("chat_id").GetString());
        Assert.Contains("renew it", doc.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public void Telegram_Url_UsesBotTokenAndSendMessage()
    {
        var url = ChatNotificationFormatter.BuildTelegramUrl("ABC123");
        Assert.Equal("https://api.telegram.org/botABC123/sendMessage", url);
    }
}
