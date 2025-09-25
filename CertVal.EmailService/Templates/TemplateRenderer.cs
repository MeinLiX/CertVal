using Fluid;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Encodings.Web;

namespace CertVal.EmailService.Templates;

public class TemplateRenderer : ITemplateRenderer
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<TemplateRenderer> _logger;

    private readonly ConcurrentDictionary<string, Lazy<Task<IFluidTemplate>>> _templateCache = new();
    private readonly FluidParser _parser = new();
    private readonly TemplateOptions _options;

    public TemplateRenderer(IHostEnvironment env, ILogger<TemplateRenderer> logger)
    {
        _fileProvider = env.ContentRootFileProvider ?? throw new ArgumentNullException(nameof(env.ContentRootFileProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options = new TemplateOptions();
        _options.MemberAccessStrategy.Register<Dictionary<string, object>>();
    }

    public async Task<string> RenderAsync(string templateName, Dictionary<string, object> data)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        var normalized = NormalizeTemplatePath(templateName);
        var isHtml = normalized.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

        var template = await GetTemplateAsync(normalized).ConfigureAwait(false);

        var safeData = SanitizeData(data, isHtml);

        var ctx = new TemplateContext(safeData, _options);
        var output = await template.RenderAsync(ctx).ConfigureAwait(false);
        return output;
    }

    private Task<IFluidTemplate> GetTemplateAsync(string normalizedTemplatePath)
    {
        var loader = _templateCache.GetOrAdd(normalizedTemplatePath, key => new Lazy<Task<IFluidTemplate>>(() => LoadTemplateAsync(key)));
        return loader.Value;
    }

    private async Task<IFluidTemplate> LoadTemplateAsync(string normalized)
    {
        var file = _fileProvider.GetFileInfo(normalized);

        if (!file.Exists)
        {
            _logger.LogError("Template not found: {TemplatePath}", normalized);
            throw new FileNotFoundException($"Template not found: {normalized}", normalized);
        }

        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);

        if (!_parser.TryParse(content, out var template, out var error))
        {
            _logger.LogError("Failed to parse template {TemplatePath}: {Error}", normalized, error);
            throw new InvalidOperationException($"Failed to parse template '{normalized}': {error}");
        }

        _logger.LogDebug("Template parsed and cached: {TemplatePath}", normalized);
        return template!;
    }

    private static string NormalizeTemplatePath(string templateName)
    {
        var relative = templateName.Replace('\\', '/');
        if (relative.StartsWith("Templates/", StringComparison.OrdinalIgnoreCase))
            relative = relative["Templates/".Length..];

        return Path.Combine("Templates", relative).Replace('\\', '/');
    }

    private static Dictionary<string, object> SanitizeData(IReadOnlyDictionary<string, object> source, bool htmlEncode)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            result[key] = ConvertValue(value, htmlEncode);
        }
        return result;
    }

    private static object ConvertValue(object? value, bool htmlEncode)
    {
        if (value is null) return string.Empty;

        if (value is System.Text.Json.JsonElement je)
        {
            return ConvertJsonElement(je, htmlEncode);
        }

        return value switch
        {
            string s => htmlEncode ? HtmlEncoder.Default.Encode(s) : s,
            bool b => b ? "true" : "false",
            int or long or short or byte or sbyte or uint or ulong or ushort => Convert.ToString(value, CultureInfo.InvariantCulture)!,
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            DateTime dt => FormatUtc(dt),
            DateTimeOffset dto => FormatUtc(dto),
            Guid g => g.ToString(),
            IDictionary<string, object> dict => SanitizeData((IReadOnlyDictionary<string, object>)dict, htmlEncode),
            IEnumerable<object> seq => string.Join(", ", seq.Select(x => ConvertValue(x, htmlEncode))),
            _ => htmlEncode ? HtmlEncoder.Default.Encode(value.ToString() ?? string.Empty) : (object)(value.ToString() ?? string.Empty)
        };
    }

    private static object ConvertJsonElement(System.Text.Json.JsonElement je, bool htmlEncode)
    {
        switch (je.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                var s = je.GetString() ?? string.Empty;
                if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                    return FormatUtc(dto);
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                    return FormatUtc(dt);
                return htmlEncode ? HtmlEncoder.Default.Encode(s) : s;

            case System.Text.Json.JsonValueKind.Number:
                if (je.TryGetInt64(out var l)) return l.ToString(CultureInfo.InvariantCulture);
                if (je.TryGetDouble(out var d)) return d.ToString(CultureInfo.InvariantCulture);
                return je.GetRawText();

            case System.Text.Json.JsonValueKind.True:
            case System.Text.Json.JsonValueKind.False:
                return je.GetBoolean() ? "true" : "false";

            case System.Text.Json.JsonValueKind.Null:
            case System.Text.Json.JsonValueKind.Undefined:
                return string.Empty;

            case System.Text.Json.JsonValueKind.Array:
                var items = je.EnumerateArray().Select(e => ConvertJsonElement(e, htmlEncode));
                return string.Join(", ", items);

            case System.Text.Json.JsonValueKind.Object:
                return je.GetRawText();

            default:
                return string.Empty;
        }
    }

    private static string FormatUtc(DateTime dt) => dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
    private static string FormatUtc(DateTimeOffset dto) => dto.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
}