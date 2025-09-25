namespace CertVal.EmailService.Templates;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, Dictionary<string, object> data);
}