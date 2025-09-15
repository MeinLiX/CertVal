using CertVal.Core.Messaging;

namespace CertVal.EmailService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailNotificationMessage message);
}