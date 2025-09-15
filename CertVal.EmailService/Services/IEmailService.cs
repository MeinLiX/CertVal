using CertVal.EmailService.Models;

namespace CertVal.EmailService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailNotificationMessage message);
}