using CertVal.Core.Messaging;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Models;
using CertVal.EmailService.Templates;
using Microsoft.Extensions.Options;

namespace CertVal.EmailService.Services;

public interface ITemplateService
{
    Task<EmailTemplate> RenderTemplateAsync(EmailNotificationMessage message);
}

public class TemplateService : ITemplateService
{
    private readonly EmailServiceConfiguration _config;
    private readonly ITemplateRenderer _templateRenderer;

    public TemplateService(
        IOptions<EmailServiceConfiguration> configuration,
        ITemplateRenderer templateRenderer)
    {
        _config = configuration.Value;
        _templateRenderer = templateRenderer;
    }

    public async Task<EmailTemplate> RenderTemplateAsync(EmailNotificationMessage message)
    {
        var templateData = CreateTemplateData(message);
        var templateName = GetTemplateName(message.Type);

        var htmlBody = await _templateRenderer.RenderAsync($"Html/{templateName}.html", templateData);
        var textBody = await _templateRenderer.RenderAsync($"Text/{templateName}.txt", templateData);
        var subject = GetSubject(message.Type);

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private Dictionary<string, object> CreateTemplateData(EmailNotificationMessage message)
    {
        var baseData = new Dictionary<string, object>
        {
            ["CompanyName"] = _config.CompanyName,
            ["SupportEmail"] = _config.SupportEmail,
            ["BaseUrl"] = _config.BaseUrl,
            ["DashboardUrl"] = $"{_config.BaseUrl}/dashboard"
        };

        foreach (var kvp in message.Data)
        {
            baseData[kvp.Key] = kvp.Value;
        }

        AddTemplateSpecificData(baseData, message.Type);

        return baseData;
    }

    private void AddTemplateSpecificData(Dictionary<string, object> data, EmailNotificationType type)
    {
        switch (type)
        {
            case EmailNotificationType.UserRegistered:
            case EmailNotificationType.EmailConfirmation:
                if (data.TryGetValue("ConfirmationToken", out var token))
                {
                    data["ConfirmationUrl"] = $"{_config.BaseUrl}/auth/confirm-email?token={Uri.EscapeDataString(token.ToString() ?? "")}";
                }
                break;

            case EmailNotificationType.PasswordReset:
                if (data.TryGetValue("ResetToken", out var resetToken))
                {
                    data["ResetUrl"] = $"{_config.BaseUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken.ToString() ?? "")}";
                }
                break;

            case EmailNotificationType.WorkspaceInvitation:
                if (data.TryGetValue("WorkspaceId", out var workspaceId) &&
                    data.TryGetValue("InvitationToken", out var inviteToken))
                {
                    data["InvitationUrl"] = $"{_config.BaseUrl}/workspaces/{workspaceId}/join?token={Uri.EscapeDataString(inviteToken.ToString() ?? "")}";
                }
                break;
        }
    }

    private static string GetTemplateName(EmailNotificationType type) => type switch
    {
        EmailNotificationType.UserRegistered => "EmailConfirmation",
        EmailNotificationType.EmailConfirmation => "EmailConfirmation",
        EmailNotificationType.PasswordReset => "PasswordReset",
        EmailNotificationType.WorkspaceInvitation => "WorkspaceInvitation",
        EmailNotificationType.CertificateExpiring => "CertificateExpiring",
        EmailNotificationType.CertificateExpired => "CertificateExpired",
        _ => throw new ArgumentException($"Unknown email notification type: {type}")
    };

    private string GetSubject(EmailNotificationType type) => type switch
    {
        EmailNotificationType.UserRegistered => $"Welcome to {_config.CompanyName}! Please confirm your email",
        EmailNotificationType.EmailConfirmation => $"Confirm your email - {_config.CompanyName}",
        EmailNotificationType.PasswordReset => $"Reset your password - {_config.CompanyName}",
        EmailNotificationType.WorkspaceInvitation => $"Workspace invitation - {_config.CompanyName}",
        EmailNotificationType.CertificateExpiring => $"Certificate expiring soon - {_config.CompanyName}",
        EmailNotificationType.CertificateExpired => $"Certificate expired - {_config.CompanyName}",
        _ => $"Notification - {_config.CompanyName}"
    };
}