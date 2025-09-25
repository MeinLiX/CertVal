using CertVal.Core.Messaging;
using CertVal.EmailService.Models;

namespace CertVal.EmailService.Services.Abstractions;

public interface ITemplateService
{
    Task<EmailTemplate> RenderTemplateAsync(EmailNotificationMessage message);
}
