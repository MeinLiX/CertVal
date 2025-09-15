using CertVal.Core.Messaging;
using CertVal.EmailService.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CertVal.EmailService.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailServiceConfiguration> configuration,
        IEmailTemplateService templateService,
        ILogger<SmtpEmailService> logger)
    {
        _smtpSettings = configuration.Value.Smtp;
        _templateService = templateService;
        _logger = logger;

        _logger.LogInformation("SMTP Configuration: Host={Host}, Port={Port}, UseSsl={UseSsl}, FromEmail={FromEmail}",
            _smtpSettings.Host, _smtpSettings.Port, _smtpSettings.UseSsl, _smtpSettings.FromEmail);
    }

    public async Task<bool> SendEmailAsync(EmailNotificationMessage message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
            {
                _logger.LogError("SMTP Host is not configured. Please check EmailService:Smtp:Host in configuration");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            {
                _logger.LogError("SMTP FromEmail is not configured. Please check EmailService:Smtp:FromEmail in configuration");
                return false;
            }

            _logger.LogInformation("Generating email template for message {MessageId} of type {Type}",
                message.MessageId, message.Type);

            var template = await _templateService.GenerateTemplateAsync(message);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            mimeMessage.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
            mimeMessage.Subject = template.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = template.HtmlBody,
                TextBody = template.TextBody
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            mimeMessage.Headers.Add("X-Message-Id", message.MessageId);
            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                mimeMessage.Headers.Add("X-Correlation-Id", message.CorrelationId);
            }

            _logger.LogInformation("Connecting to SMTP server {Host}:{Port}", _smtpSettings.Host, _smtpSettings.Port);

            using var client = new SmtpClient();

            var sslOptions = GetSslOptions();
            _logger.LogDebug("Using SSL options: {SslOptions} for port {Port}", sslOptions, _smtpSettings.Port);

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, sslOptions);

            if (!string.IsNullOrEmpty(_smtpSettings.Username))
            {
                _logger.LogDebug("Authenticating with SMTP server using username: {Username}", _smtpSettings.Username);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }
            else
            {
                _logger.LogDebug("No SMTP authentication configured");
            }

            _logger.LogInformation("Sending email to {ToEmail} with subject: {Subject}", message.ToEmail, template.Subject);

            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. MessageId: {MessageId}, Type: {Type}, To: {Email}. Error: {Error}",
                message.MessageId, message.Type, message.ToEmail, ex.Message);
            return false;
        }
    }

    private SecureSocketOptions GetSslOptions()
    {
        if (!string.IsNullOrEmpty(_smtpSettings.SslMode))
        {
            return _smtpSettings.SslMode.ToLowerInvariant() switch
            {
                "none" => SecureSocketOptions.None,
                "starttls" => SecureSocketOptions.StartTls,
                "sslonconnect" => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.Auto
            };
        }

        return _smtpSettings.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            25 => SecureSocketOptions.StartTlsWhenAvailable,
            _ => _smtpSettings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
        };
    }
}