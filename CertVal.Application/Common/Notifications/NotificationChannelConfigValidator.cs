using CertVal.Application.Common.Models;
using CertVal.Application.DTOs;
using CertVal.Core.Enums;
using System.Text.Json;

namespace CertVal.Application.Common.Notifications;

/// <summary>
/// Parses and validates the per-channel notification configuration JSON, returning a
/// normalized config string when valid or a descriptive error otherwise. Pure and
/// dependency-free so it can be exercised by unit tests and reused by command handlers.
/// </summary>
public static class NotificationChannelConfigValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Result<string> Validate(NotificationChannelType channelType, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result.Failure<string>("Channel configuration is required.");

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Result.Failure<string>("Channel configuration must be valid JSON.");
        }

        return channelType switch
        {
            NotificationChannelType.Email => ValidateEmail(root),
            NotificationChannelType.Webhook => ValidateWebhook(root),
            NotificationChannelType.Slack => ValidateSlack(root),
            NotificationChannelType.Telegram => ValidateTelegram(root),
            _ => Result.Failure<string>($"Channel type '{channelType}' is not supported.")
        };
    }

    private static Result<string> ValidateEmail(JsonElement root)
    {
        if (!root.TryGetProperty("userIds", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Result.Failure<string>("Email configuration must contain a 'userIds' array.");

        var ids = new List<Guid>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String || !Guid.TryParse(el.GetString(), out var id))
                return Result.Failure<string>("Each 'userIds' entry must be a valid GUID.");
            if (!ids.Contains(id)) ids.Add(id);
        }

        if (ids.Count == 0)
            return Result.Failure<string>("At least one recipient user ID is required.");

        return Result.Success(JsonSerializer.Serialize(new EmailChannelConfig(ids), SerializerOptions));
    }

    private static Result<string> ValidateWebhook(JsonElement root)
    {
        if (!root.TryGetProperty("url", out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
            return Result.Failure<string>("Webhook configuration must contain a 'url'.");

        var url = urlEl.GetString()?.Trim();
        if (!IsAbsoluteHttpUrl(url))
            return Result.Failure<string>("Webhook 'url' must be an absolute http(s) URL.");

        Dictionary<string, string>? headers = null;
        if (root.TryGetProperty("headers", out var headersEl) && headersEl.ValueKind == JsonValueKind.Object)
        {
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in headersEl.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.String)
                    return Result.Failure<string>($"Header '{prop.Name}' must be a string value.");
                headers[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            if (headers.Count == 0) headers = null;
        }

        return Result.Success(JsonSerializer.Serialize(new WebhookChannelConfig(url!, headers), SerializerOptions));
    }

    private static Result<string> ValidateSlack(JsonElement root)
    {
        if (!root.TryGetProperty("webhookUrl", out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
            return Result.Failure<string>("Slack configuration must contain a 'webhookUrl'.");

        var url = urlEl.GetString()?.Trim();
        if (!IsAbsoluteHttpUrl(url) || !url!.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<string>("Slack 'webhookUrl' must be an absolute https URL.");

        return Result.Success(JsonSerializer.Serialize(new SlackChannelConfig(url), SerializerOptions));
    }

    private static Result<string> ValidateTelegram(JsonElement root)
    {
        var botToken = root.TryGetProperty("botToken", out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String
            ? tokenEl.GetString()?.Trim()
            : null;
        var chatId = root.TryGetProperty("chatId", out var chatEl) && chatEl.ValueKind == JsonValueKind.String
            ? chatEl.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(botToken))
            return Result.Failure<string>("Telegram configuration must contain a non-empty 'botToken'.");
        if (string.IsNullOrWhiteSpace(chatId))
            return Result.Failure<string>("Telegram configuration must contain a non-empty 'chatId'.");

        return Result.Success(JsonSerializer.Serialize(new TelegramChannelConfig(botToken, chatId), SerializerOptions));
    }

    private static bool IsAbsoluteHttpUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
