using CertVal.EmailService.Configuration;
using CertVal.EmailService.Models;
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
    }

    public async Task<bool> SendEmailAsync(EmailNotificationMessage message)
    {
        try
        {
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

            using var client = new SmtpClient();

            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port,
                _smtpSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(_smtpSettings.Username))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }

            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. MessageId: {MessageId}, Type: {Type}, To: {Email}",
                message.MessageId, message.Type, message.ToEmail);
            return false;
        }
    }
}