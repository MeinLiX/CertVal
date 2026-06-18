using System.Text.Json;

namespace CertVal.Application.Common.Notifications;

/// <summary>
/// Pure builders for chat-channel notification payloads (Slack incoming webhooks
/// and the Telegram Bot API). Kept dependency-free so the exact JSON shape is
/// unit-testable; delivery is performed by the infrastructure event handler.
/// </summary>
public static class ChatNotificationFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Builds the JSON body for a Slack incoming webhook: <c>{ "text": ... }</c>.</summary>
    public static string BuildSlackPayload(string subject, string message)
    {
        var text = $"*{subject}*\n{message}";
        return JsonSerializer.Serialize(new { text }, Options);
    }

    /// <summary>Builds the JSON body for the Telegram <c>sendMessage</c> API.</summary>
    public static string BuildTelegramPayload(string chatId, string subject, string message)
    {
        var text = $"{subject}\n{message}";
        // snake_case keys are required by the Telegram Bot API, so serialize an explicit map.
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["disable_web_page_preview"] = true
        });
    }

    /// <summary>Telegram sendMessage endpoint for a given bot token.</summary>
    public static string BuildTelegramUrl(string botToken)
        => $"https://api.telegram.org/bot{botToken}/sendMessage";
}
