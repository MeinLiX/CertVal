using CertVal.EmailService.Models;

namespace CertVal.EmailService.Services;

public interface IEmailTemplateService
{
    Task<EmailTemplate> GenerateTemplateAsync(EmailNotificationMessage message);
}

public record EmailTemplate
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
}