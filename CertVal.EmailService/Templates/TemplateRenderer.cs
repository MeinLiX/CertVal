namespace CertVal.EmailService.Templates;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, Dictionary<string, object> data);
}

public class TemplateRenderer : ITemplateRenderer
{
    private readonly string _contentRootPath;
    private readonly Dictionary<string, string> _templateCache = new();
    private readonly ILogger<TemplateRenderer> _logger;

    public TemplateRenderer(ILogger<TemplateRenderer> logger)
    {
        _contentRootPath = Directory.GetCurrentDirectory();
        _logger = logger;
    }

    public async Task<string> RenderAsync(string templateName, Dictionary<string, object> data)
    {
        var template = await GetTemplateAsync(templateName);
        return RenderTemplate(template, data);
    }

    private async Task<string> GetTemplateAsync(string templateName)
    {
        if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
            return cachedTemplate;

        var templatePath = Path.Combine(_contentRootPath, "Templates", templateName);

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Template not found: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Template not found: {templateName}");
        }

        var content = await File.ReadAllTextAsync(templatePath);
        _templateCache[templateName] = content;

        return content;
    }

    private static string RenderTemplate(string template, Dictionary<string, object> data)
    {
        var result = template;

        foreach (var kvp in data)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            var value = kvp.Value?.ToString() ?? string.Empty;
            result = result.Replace(placeholder, value);
        }

        return result;
    }
}