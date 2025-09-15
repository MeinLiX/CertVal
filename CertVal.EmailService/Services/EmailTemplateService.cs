using CertVal.EmailService.Configuration;
using CertVal.EmailService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CertVal.EmailService.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly EmailTemplateSettings _templateSettings;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        IOptions<EmailServiceConfiguration> configuration,
        ILogger<EmailTemplateService> logger)
    {
        _templateSettings = configuration.Value.Templates;
        _logger = logger;
    }

    public async Task<EmailTemplate> GenerateTemplateAsync(EmailNotificationMessage message)
    {
        return message.Type switch
        {
            EmailNotificationType.UserRegistered => GenerateUserRegisteredTemplate(message),
            EmailNotificationType.EmailConfirmation => GenerateEmailConfirmationTemplate(message),
            EmailNotificationType.PasswordReset => GeneratePasswordResetTemplate(message),
            EmailNotificationType.WorkspaceInvitation => GenerateWorkspaceInvitationTemplate(message),
            EmailNotificationType.CertificateExpiring => GenerateCertificateExpiringTemplate(message),
            EmailNotificationType.CertificateExpired => GenerateCertificateExpiredTemplate(message),
            _ => throw new NotSupportedException($"Email template for type {message.Type} is not supported")
        };
    }

    private EmailTemplate GenerateUserRegisteredTemplate(EmailNotificationMessage message)
    {
        var data = ExtractData<UserRegisteredData>(message.Data);
        var confirmationUrl = $"{data.BaseUrl}/auth/confirm-email?token={data.ConfirmationToken}";

        var subject = $"Welcome to {_templateSettings.CompanyName}! Please confirm your email";

        // Використовуємо $$""" для того, щоб компілятор не плутав фігурні дужки CSS {} з інтерполяцією C# {}.
        // Тепер для інтерполяції ми використовуємо подвійні фігурні дужки {{змінна}}.
        var htmlBody = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Welcome to {{_templateSettings.CompanyName}}</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { color: white; margin: 0; font-size: 28px; }
                    .content { background: #ffffff; padding: 40px; border: 1px solid #e0e0e0; }
                    .button { display: inline-block; padding: 15px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }
                    .button:hover { background: #5a6fd8; }
                    .footer { background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }
                    .warning { background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>Welcome to {{_templateSettings.CompanyName}}!</h1>
                </div>
                <div class="content">
                    <h2>Hello {{data.FirstName}}!</h2>
                    <p>Thank you for registering with {{_templateSettings.CompanyName}}. We're excited to help you monitor and manage your SSL certificates efficiently.</p>
                    
                    <p>To complete your registration and activate your account, please confirm your email address by clicking the button below:</p>
                    
                    <div style="text-align: center;">
                        <a href="{{confirmationUrl}}" class="button">Confirm Email Address</a>
                    </div>
                    
                    <div class="warning">
                        <strong>Security Note:</strong> This confirmation link will expire in 24 hours. If you didn't create this account, you can safely ignore this email.
                    </div>
                    
                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p style="word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 5px;">{{confirmationUrl}}</p>
                    
                    <p>Once confirmed, you'll be able to:</p>
                    <ul>
                        <li>Create workspaces for organizing your certificates</li>
                        <li>Upload and monitor SSL certificates</li>
                        <li>Set up automated expiry notifications</li>
                        <li>Generate detailed reports</li>
                    </ul>
                    
                    <p>If you have any questions, feel free to contact our support team at <a href="mailto:{{_templateSettings.SupportEmail}}">{{_templateSettings.SupportEmail}}</a>.</p>
                    
                    <p>Best regards,<br>The {{_templateSettings.CompanyName}} Team</p>
                </div>
                <div class="footer">
                    <p>&copy; 2025 {{_templateSettings.CompanyName}}. All rights reserved.</p>
                    <p>This email was sent to {{data.Email}}</p>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            Welcome to {_templateSettings.CompanyName}!

            Hello {data.FirstName}!

            Thank you for registering with {_templateSettings.CompanyName}. To complete your registration and activate your account, please confirm your email address by visiting:

            {confirmationUrl}

            This confirmation link will expire in 24 hours. If you didn't create this account, you can safely ignore this email.

            Once confirmed, you'll be able to create workspaces, upload certificates, set up notifications, and more.

            If you have any questions, contact us at {_templateSettings.SupportEmail}.

            Best regards,
            The {_templateSettings.CompanyName} Team
            """;

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private EmailTemplate GenerateWorkspaceInvitationTemplate(EmailNotificationMessage message)
    {
        var data = ExtractData<WorkspaceInvitationData>(message.Data);
        var invitationUrl = $"{data.BaseUrl}/workspaces/{data.WorkspaceId}/join?token={data.InvitationToken}";

        var subject = $"You've been invited to join '{data.WorkspaceName}' workspace";

        var htmlBody = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Workspace Invitation</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { color: white; margin: 0; font-size: 28px; }
                    .content { background: #ffffff; padding: 40px; border: 1px solid #e0e0e0; }
                    .button { display: inline-block; padding: 15px 30px; background: #11998e; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }
                    .button:hover { background: #0f8174; }
                    .workspace-info { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #11998e; }
                    .footer { background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>Workspace Invitation</h1>
                </div>
                <div class="content">
                    <h2>You've been invited!</h2>
                    <p>Hello {{data.InviteeName}},</p>
                    
                    <p><strong>{{data.InviterName}}</strong> has invited you to join the <strong>'{{data.WorkspaceName}}'</strong> workspace on {{_templateSettings.CompanyName}}.</p>
                    
                    <div class="workspace-info">
                        <h3>Workspace Details</h3>
                        <p><strong>Workspace:</strong> {{data.WorkspaceName}}</p>
                        <p><strong>Invited by:</strong> {{data.InviterName}}</p>
                        <p><strong>Your role:</strong> {{data.Role}}</p>
                    </div>
                    
                    <p>As a {{data.Role.ToLower()}}, you'll be able to collaborate on certificate management and monitoring within this workspace.</p>
                    
                    <div style="text-align: center;">
                        <a href="{{invitationUrl}}" class="button">Accept Invitation</a>
                    </div>
                    
                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p style="word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 5px;">{{invitationUrl}}</p>
                    
                    <p><strong>Note:</strong> This invitation link will expire in 7 days. If you don't have a {{_templateSettings.CompanyName}} account yet, you'll be prompted to create one.</p>
                    
                    <p>If you have any questions about this invitation, please contact {{data.InviterName}} or our support team at <a href="mailto:{{_templateSettings.SupportEmail}}">{{_templateSettings.SupportEmail}}</a>.</p>
                    
                    <p>Best regards,<br>The {{_templateSettings.CompanyName}} Team</p>
                </div>
                <div class="footer">
                    <p>&copy; 2025 {{_templateSettings.CompanyName}}. All rights reserved.</p>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            Workspace Invitation

            Hello {data.InviteeName},

            {data.InviterName} has invited you to join the '{data.WorkspaceName}' workspace on {_templateSettings.CompanyName}.

            Workspace Details:
            - Workspace: {data.WorkspaceName}
            - Invited by: {data.InviterName}
            - Your role: {data.Role}

            To accept this invitation, visit: {invitationUrl}

            This invitation link will expire in 7 days. If you don't have a {_templateSettings.CompanyName} account yet, you'll be prompted to create one.

            If you have any questions, contact us at {_templateSettings.SupportEmail}.

            Best regards,
            The {_templateSettings.CompanyName} Team
            """;

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private EmailTemplate GeneratePasswordResetTemplate(EmailNotificationMessage message)
    {
        var data = ExtractData<PasswordResetData>(message.Data);
        var resetUrl = $"{data.BaseUrl}/auth/reset-password?token={data.ResetToken}";

        var subject = "Password Reset Request";

        var htmlBody = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Password Reset</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { color: white; margin: 0; font-size: 28px; }
                    .content { background: #ffffff; padding: 40px; border: 1px solid #e0e0e0; }
                    .button { display: inline-block; padding: 15px 30px; background: #ff6b6b; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }
                    .button:hover { background: #ff5252; }
                    .warning { background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }
                    .footer { background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>Password Reset</h1>
                </div>
                <div class="content">
                    <h2>Hello {{data.FirstName}}!</h2>
                    <p>We received a request to reset your password for your {{_templateSettings.CompanyName}} account.</p>
                    
                    <div style="text-align: center;">
                        <a href="{{resetUrl}}" class="button">Reset Password</a>
                    </div>
                    
                    <div class="warning">
                        <strong>Security Information:</strong>
                        <ul>
                            <li>This reset link will expire on {{data.ExpiresAt:MMM dd, yyyy 'at' HH:mm}} UTC</li>
                            <li>If you didn't request this reset, please ignore this email</li>
                            <li>For security, this link can only be used once</li>
                        </ul>
                    </div>
                    
                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p style="word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 5px;">{{resetUrl}}</p>
                    
                    <p>If you have any concerns about account security, please contact our support team immediately at <a href="mailto:{{_templateSettings.SupportEmail}}">{{_templateSettings.SupportEmail}}</a>.</p>
                    
                    <p>Best regards,<br>The {{_templateSettings.CompanyName}} Team</p>
                </div>
                <div class="footer">
                    <p>&copy; 2025 {{_templateSettings.CompanyName}}. All rights reserved.</p>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            Password Reset Request

            Hello {data.FirstName}!

            We received a request to reset your password for your {_templateSettings.CompanyName} account.

            To reset your password, visit: {resetUrl}

            Security Information:
            - This reset link will expire on {data.ExpiresAt:MMM dd, yyyy 'at' HH:mm} UTC
            - If you didn't request this reset, please ignore this email
            - For security, this link can only be used once

            If you have any concerns, contact us at {_templateSettings.SupportEmail}.

            Best regards,
            The {_templateSettings.CompanyName} Team
            """;

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private EmailTemplate GenerateCertificateExpiringTemplate(EmailNotificationMessage message)
    {
        var data = ExtractData<CertificateExpiringData>(message.Data);
        var dashboardUrl = $"{data.BaseUrl}/dashboard";

        var urgencyLevel = data.DaysUntilExpiry switch
        {
            <= 7 => "🔴 CRITICAL",
            <= 30 => "🟡 WARNING",
            _ => "🔵 INFO"
        };

        var subject = $"{urgencyLevel} Certificate expires in {data.DaysUntilExpiry} days: {data.CertificateSubject}";

        var htmlBody = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Certificate Expiry Alert</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #f39c12 0%, #e74c3c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { color: white; margin: 0; font-size: 28px; }
                    .content { background: #ffffff; padding: 40px; border: 1px solid #e0e0e0; }
                    .button { display: inline-block; padding: 15px 30px; background: #e74c3c; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }
                    .cert-details { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #e74c3c; }
                    .footer { background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>{{urgencyLevel}} Certificate Alert</h1>
                </div>
                <div class="content">
                    <h2>Certificate Expiring Soon</h2>
                    <p>A certificate in your <strong>{{data.WorkspaceName}}</strong> workspace is expiring in <strong>{{data.DaysUntilExpiry}} day(s)</strong>.</p>
                    
                    <div class="cert-details">
                        <h3>Certificate Details</h3>
                        <p><strong>Subject:</strong> {{data.CertificateSubject}}</p>
                        <p><strong>Issuer:</strong> {{data.CertificateIssuer}}</p>
                        <p><strong>Expires On:</strong> {{data.ExpiryDate:MMM dd, yyyy 'at' HH:mm}} UTC</p>
                        <p><strong>Days Remaining:</strong> {{data.DaysUntilExpiry}}</p>
                        <p><strong>Workspace:</strong> {{data.WorkspaceName}}</p>
                    </div>
                    
                    <p><strong>Action Required:</strong> Please renew this certificate as soon as possible to avoid service disruption.</p>
                    
                    <div style="text-align: center;">
                        <a href="{{dashboardUrl}}" class="button">View Dashboard</a>
                    </div>
                    
                    <p>Best regards,<br>The {{_templateSettings.CompanyName}} Monitoring System</p>
                </div>
                <div class="footer">
                    <p>&copy; 2025 {{_templateSettings.CompanyName}}. All rights reserved.</p>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            {urgencyLevel} Certificate Alert

            A certificate in your {data.WorkspaceName} workspace is expiring in {data.DaysUntilExpiry} day(s).

            Certificate Details:
            - Subject: {data.CertificateSubject}
            - Issuer: {data.CertificateIssuer}
            - Expiry Date: {data.ExpiryDate:MMM dd, yyyy 'at' HH:mm} UTC
            - Days Until Expiry: {data.DaysUntilExpiry}
            - Workspace: {data.WorkspaceName}

            Action Required: Please renew this certificate as soon as possible to avoid service disruption.

            View your dashboard: {dashboardUrl}

            Best regards,
            The {_templateSettings.CompanyName} Monitoring System
            """;

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private EmailTemplate GenerateCertificateExpiredTemplate(EmailNotificationMessage message)
    {
        var data = ExtractData<CertificateExpiringData>(message.Data);
        var dashboardUrl = $"{data.BaseUrl}/dashboard";

        var subject = $"🔴 EXPIRED: Certificate has expired - {data.CertificateSubject}";

        var htmlBody = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Certificate Expired</title>
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .header h1 { color: white; margin: 0; font-size: 28px; }
                    .content { background: #ffffff; padding: 40px; border: 1px solid #e0e0e0; }
                    .button { display: inline-block; padding: 15px 30px; background: #e74c3c; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }
                    .cert-details { background: #fff5f5; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #e74c3c; }
                    .urgent { background: #ffebee; border: 2px solid #e74c3c; padding: 20px; border-radius: 8px; margin: 20px 0; }
                    .footer { background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 10px 10px; }
                </style>
            </head>
            <body>
                <div class="header">
                    <h1>🔴 Certificate Expired</h1>
                </div>
                <div class="content">
                    <div class="urgent">
                        <h2>⚠️ IMMEDIATE ACTION REQUIRED</h2>
                        <p>A certificate in your <strong>{{data.WorkspaceName}}</strong> workspace has <strong>EXPIRED</strong>.</p>
                    </div>
                    
                    <div class="cert-details">
                        <h3>Certificate Details</h3>
                        <p><strong>Subject:</strong> {{data.CertificateSubject}}</p>
                        <p><strong>Issuer:</strong> {{data.CertificateIssuer}}</p>
                        <p><strong>Expired On:</strong> {{data.ExpiryDate:MMM dd, yyyy 'at' HH:mm}} UTC</p>
                        <p><strong>Days Overdue:</strong> {{Math.Abs(data.DaysUntilExpiry)}}</p>
                        <p><strong>Workspace:</strong> {{data.WorkspaceName}}</p>
                    </div>
                    
                    <p><strong>CRITICAL:</strong> This certificate has expired and may be causing service disruptions. Please renew immediately.</p>
                    
                    <div style="text-align: center;">
                        <a href="{{dashboardUrl}}" class="button">View Dashboard</a>
                    </div>
                    
                    <p>Best regards,<br>The {{_templateSettings.CompanyName}} Monitoring System</p>
                </div>
                <div class="footer">
                    <p>&copy; 2025 {{_templateSettings.CompanyName}}. All rights reserved.</p>
                </div>
            </body>
            </html>
            """;

        var textBody = $"""
            🔴 CERTIFICATE EXPIRED

            IMMEDIATE ACTION REQUIRED

            A certificate in your {data.WorkspaceName} workspace has EXPIRED.

            Certificate Details:
            - Subject: {data.CertificateSubject}
            - Issuer: {data.CertificateIssuer}
            - Expired On: {data.ExpiryDate:MMM dd, yyyy 'at' HH:mm} UTC
            - Days Overdue: {Math.Abs(data.DaysUntilExpiry)}
            - Workspace: {data.WorkspaceName}

            CRITICAL: This certificate has expired and may be causing service disruptions. Please renew immediately.

            View your dashboard: {dashboardUrl}

            Best regards,
            The {_templateSettings.CompanyName} Monitoring System
            """;

        return new EmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }

    private EmailTemplate GenerateEmailConfirmationTemplate(EmailNotificationMessage message)
    {
        return GenerateUserRegisteredTemplate(message);
    }

    private T ExtractData<T>(Dictionary<string, object> data) where T : new()
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }) ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract data of type {Type}", typeof(T).Name);
            return new T();
        }
    }
}
