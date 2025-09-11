using CertVal.Application.Common.Models;
using CertVal.Core.Entities;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace CertVal.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> SendNotificationAsync(NotificationHistory notification, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (notification.ChannelType)
            {
                case NotificationChannelType.Email:
                    return await SendEmailNotificationAsync(notification, cancellationToken);

                case NotificationChannelType.Webhook:
                    return await SendWebhookNotificationAsync(notification, cancellationToken);

                case NotificationChannelType.Slack:
                case NotificationChannelType.Telegram:
                    return Result.Failure($"Channel type {notification.ChannelType} not implemented yet");

                default:
                    return Result.Failure($"Unknown channel type: {notification.ChannelType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
            return Result.Failure($"Notification failed: {ex.Message}");
        }
    }

    public async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var pendingNotifications = await _unitOfWork.NotificationHistory.GetPendingAsync(cancellationToken);

        _logger.LogInformation("Processing {Count} pending notifications", pendingNotifications.Count());

        foreach (var notification in pendingNotifications)
        {
            try
            {
                var result = await SendNotificationAsync(notification, cancellationToken);

                if (result.IsSuccess)
                {
                    notification.MarkAsSent();
                    _logger.LogInformation("Notification {NotificationId} sent successfully", notification.Id);
                }
                else
                {
                    notification.MarkAsFailed(result.Error);
                    _logger.LogWarning("Notification {NotificationId} failed: {Error}",
                        notification.Id, result.Error);
                }
            }
            catch (Exception ex)
            {
                notification.MarkAsFailed(ex.Message);
                _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> SendEmailNotificationAsync(NotificationHistory notification, CancellationToken cancellationToken)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var smtpHost = emailSettings["SmtpHost"];
        var smtpPort = int.TryParse(emailSettings["SmtpPort"], out var port) ? port : 587;
        var username = emailSettings["Username"];
        var password = emailSettings["Password"];
        var fromEmail = emailSettings["FromEmail"];
        var fromName = emailSettings["FromName"] ?? "CertVal";

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(username))
        {
            _logger.LogInformation("Email would be sent to {Recipient}: {Subject}",
                notification.Recipient, notification.Subject);
            return Result.Success();
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail ?? username, fromName),
                Subject = notification.Subject,
                Body = notification.Message,
                IsBodyHtml = true
            };

            message.To.Add(notification.Recipient);

            await client.SendMailAsync(message, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Email sending failed: {ex.Message}");
        }
    }

    private async Task<Result> SendWebhookNotificationAsync(NotificationHistory notification, CancellationToken cancellationToken)
    {
        try
        {
            var config = JsonSerializer.Deserialize<WebhookConfig>(notification.NotificationRule.ChannelConfig);
            if (config?.Url == null)
                return Result.Failure("Webhook URL not configured");

            using var client = new HttpClient();

            var payload = new
            {
                notification.Subject,
                notification.Message,
                notification.CertificateId,
                notification.ScheduledAt,
                Certificate = new
                {
                    notification.Certificate.Subject,
                    notification.Certificate.NotAfter,
                    DaysUntilExpiry = (notification.Certificate.NotAfter - DateTime.UtcNow).Days
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(config.Url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
                return Result.Success();
            else
                return Result.Failure($"Webhook returned {response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Webhook failed: {ex.Message}");
        }
    }

    private class WebhookConfig
    {
        public string? Url { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }
}