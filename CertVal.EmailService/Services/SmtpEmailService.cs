using CertVal.Core.Messaging;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Services.Abstractions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CertVal.EmailService.Services;

public class SmtpEmailService : IEmailService, IDisposable
{
    private readonly EmailServiceConfiguration _config;
    private readonly SmtpSettings _smtpSettings;
    private readonly ITemplateService _templateService;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly EmailServiceMetrics _metrics;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private SmtpClient? _smtpClient;
    private DateTime _lastUsed = DateTime.MinValue;
    private readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(2);
    private bool _disposed;

    public SmtpEmailService(
        IOptions<EmailServiceConfiguration> configuration,
        ITemplateService templateService,
        ILogger<SmtpEmailService> logger,
        EmailServiceMetrics metrics)
    {
        _config = configuration.Value;
        _smtpSettings = _config.Smtp;
        _templateService = templateService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> SendEmailAsync(EmailNotificationMessage message)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["MessageId"] = message.MessageId,
            ["CorrelationId"] = message.CorrelationId
        });

        try
        {
            ValidateConfiguration();
            ValidateMessage(message);

            var template = await _templateService.RenderTemplateAsync(message).ConfigureAwait(false);
            var mimeMessage = CreateMimeMessage(message, template);

            await SendEmailInternal(mimeMessage).ConfigureAwait(false);

            if (message.IsAggregated)
            {
                _logger.LogInformation("Aggregated email sent successfully to {Count} recipients (primary: {Primary})", message.Recipients!.Count, message.ToEmail);
            }
            else
            {
                _logger.LogInformation("Email sent successfully to {ToEmail}", message.ToEmail);
            }

            _metrics.RecordEmailSent(message.Type.ToString(), message.IsAggregated);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email (aggregated={Aggregated}) to {ToEmail}: {Error}", message.IsAggregated, message.ToEmail, ex.Message);
            return false;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
            throw new InvalidOperationException("SMTP Host is not configured");

        if (string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            throw new InvalidOperationException("SMTP FromEmail is not configured");

        if (_smtpSettings.Port <= 0 || _smtpSettings.Port > 65535)
            throw new InvalidOperationException("SMTP Port is invalid");

        if (!MailboxAddress.TryParse(_smtpSettings.FromEmail, out _))
            throw new InvalidOperationException("SMTP FromEmail is invalid");
    }

    private static void ValidateMessage(EmailNotificationMessage message)
    {
        if (message.IsAggregated)
        {
            if (message.Recipients is null || message.Recipients.Count == 0)
                throw new ArgumentException("Aggregated message requires recipients list", nameof(message));
            if (string.IsNullOrWhiteSpace(message.ToEmail))
                throw new ArgumentException("Primary ToEmail is required for aggregated message (can be placeholder)", nameof(message));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(message.ToEmail))
                throw new ArgumentException("Recipient email is required", nameof(message));
            if (!MailboxAddress.TryParse(message.ToEmail, out _))
                throw new ArgumentException("Recipient email is invalid", nameof(message));
        }
    }

    private MimeMessage CreateMimeMessage(EmailNotificationMessage message, Models.EmailTemplate template)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = template.Subject,
        };

        var from = CreateMailbox(_smtpSettings.FromName, _smtpSettings.FromEmail);
        mimeMessage.From.Add(from);

        var to = CreateMailbox(message.ToName, message.ToEmail);
        mimeMessage.To.Add(to);

        if (message.IsAggregated && message.Recipients is { Count: > 0 })
        {
            foreach (var rcpt in message.Recipients)
            {
                if (string.Equals(rcpt, message.ToEmail, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (MailboxAddress.TryParse(rcpt, out var mb))
                {
                    if (_config.AggregatedUseBcc)
                        mimeMessage.Bcc.Add(mb);
                    else
                        mimeMessage.To.Add(mb);
                }
            }
        }


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

        return mimeMessage;
    }

    private static MailboxAddress CreateMailbox(string? name, string email)
    {
        if (!MailboxAddress.TryParse(email, out var parsed))
            throw new InvalidOperationException("Invalid email address format");

        return string.IsNullOrWhiteSpace(name)
            ? new MailboxAddress(parsed.Name, parsed.Address)
            : new MailboxAddress(name, parsed.Address);
    }

    //A simple solution for a small project. Implementing a connection pool would be a better
    private async Task SendEmailInternal(MimeMessage message)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            var client = await GetOrCreateSmtpClientAsync();
            await client.SendAsync(message).ConfigureAwait(false);
            _lastUsed = DateTime.UtcNow;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task<SmtpClient> GetOrCreateSmtpClientAsync()
    {
        if (_smtpClient?.IsConnected == true &&
            DateTime.UtcNow - _lastUsed > _connectionTimeout)
        {
            _logger.LogDebug("SMTP connection idle timeout, disconnecting");
            await DisconnectSmtpClientAsync();
        }

        if (_smtpClient?.IsConnected == true)
        {
            return _smtpClient;
        }

        _smtpClient?.Dispose();
        _smtpClient = null;

        _logger.LogDebug("Creating new SMTP connection to {Host}:{Port}", _smtpSettings.Host, _smtpSettings.Port);
        var client = new SmtpClient();

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.GetSslOptions)
                    .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_smtpSettings.Username))
        {
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password)
                        .ConfigureAwait(false);
        }

        _smtpClient = client;
        _lastUsed = DateTime.UtcNow;
        _metrics.RecordSmtpConnectionCreated();
        _logger.LogInformation("SMTP connection established successfully");
        return client;
    }

    private async Task DisconnectSmtpClientAsync()
    {
        if (_smtpClient?.IsConnected == true)
        {
            try
            {
                await _smtpClient.DisconnectAsync(true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting SMTP client");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connectionSemaphore.Wait();
        try
        {
            DisconnectSmtpClientAsync().GetAwaiter().GetResult();
            _smtpClient?.Dispose();
            _smtpClient = null;
        }
        finally
        {
            _connectionSemaphore.Release();
            _connectionSemaphore.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}