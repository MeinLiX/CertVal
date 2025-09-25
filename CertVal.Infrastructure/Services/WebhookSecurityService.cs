using CertVal.Application.Common.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace CertVal.Infrastructure.Services;

public sealed partial class WebhookSecurityService : IWebhookSecurityService
{
    private static readonly string[] ForbiddenHostnames =
    [
        "localhost", "localhost.localdomain", "127.0.0.1", "::1"
    ];

    private static readonly HashSet<int> AllowedPorts = new() { 80, 443 };

    [GeneratedRegex(@"^(?=.{1,253}$)(?!-)[A-Za-z0-9-]{1,63}(?<!-)(\.(?!-)[A-Za-z0-9-]{1,63}(?<!-))*$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex HostnameRegex();

    public async Task<(bool IsValid, Uri? Uri, string? Error)> ValidateUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, null, "URL is required");

        if (url.Length > 2048)
            return (false, null, "URL too long");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, null, "Invalid URL format");

        if (uri.Scheme != Uri.UriSchemeHttps)
            return (false, null, "Only HTTPS scheme is allowed");

        if (!AllowedPorts.Contains(uri.IsDefaultPort ? 443 : uri.Port))
            return (false, null, "Port not allowed");

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return (false, null, "User info / credentials not allowed in URL");

        if (!string.IsNullOrEmpty(uri.Fragment))
            return (false, null, "URL fragments are not allowed");

        var host = uri.Host.Trim().ToLowerInvariant();

        if (ForbiddenHostnames.Contains(host))
            return (false, null, "Host not allowed");

        if (!HostnameRegex().IsMatch(host) && !IPAddress.TryParse(host, out _))
            return (false, null, "Invalid host name");

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            if (addresses.Length == 0)
                return (false, null, "Host did not resolve");

            foreach (var ip in addresses)
            {
                if (IsDisallowedAddress(ip))
                    return (false, null, $"Resolved to disallowed address {ip}");
            }
        }
        catch (SocketException)
        {
            return (false, null, "DNS resolution failed");
        }
        catch (Exception ex)
        {
            return (false, null, $"DNS error: {ex.Message}");
        }

        return (true, uri, null);
    }

    public IDictionary<string, string> SanitizeHeaders(IDictionary<string, string>? headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers == null) return result;

        const int maxHeaders = 20;
        const int maxTotalSize = 4096;
        var totalSize = 0;

        foreach (var kvp in headers)
        {
            if (result.Count >= maxHeaders) break;
            if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
            if (IsRestrictedHeader(kvp.Key)) continue;

            var cleanKey = SanitizeToken(kvp.Key, 50);
            if (cleanKey.Length == 0) continue;

            var cleanValue = SanitizeHeaderValue(kvp.Value, 256);
            if (cleanValue.Length == 0) continue;

            totalSize += cleanKey.Length + cleanValue.Length;
            if (totalSize > maxTotalSize) break;

            result[cleanKey] = cleanValue;
        }
        return result;
    }

    public string SanitizeValue(string? value, int maxLength = 512)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var ch in value)
        {
            if (char.IsControl(ch) && ch != '\n' && ch != '\r' && ch != '\t') continue;
            sb.Append(ch);
            if (sb.Length >= maxLength) break;
        }
        return sb.ToString().Trim();
    }

    private static string SanitizeHeaderValue(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var ch in value)
        {
            if (ch == '\r' || ch == '\n' || ch == '\t') continue;
            if (char.IsControl(ch)) continue;
            sb.Append(ch);
            if (sb.Length >= maxLength) break;
        }
        return sb.ToString().Trim();
    }

    private static string SanitizeToken(string token, int maxLength)
    {
        var filtered = new string(token.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        if (filtered.Length > maxLength) filtered = filtered[..maxLength];
        return filtered;
    }

    private static bool IsRestrictedHeader(string headerName)
    {
        return headerName.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
               headerName.StartsWith("Proxy-", StringComparison.OrdinalIgnoreCase) ||
               headerName.StartsWith("Sec-", StringComparison.OrdinalIgnoreCase) ||
               headerName.StartsWith("Transfer-", StringComparison.OrdinalIgnoreCase) ||
               headerName.StartsWith("TE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDisallowedAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv4MappedToIPv6)
        {
            var mapped = ip.MapToIPv4();
            if (IsDisallowedAddress(mapped)) return true;
        }

        if (ip.IsIPv6Multicast || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 0) return true;                                 // 0.0.0.0/8
            if (b[0] == 10) return true;                                // 10.0.0.0/8
            if (b[0] == 127) return true;                               // 127.0.0.0/8
            if (b[0] == 169 && b[1] == 254) return true;                // 169.254.0.0/16
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;   // 172.16.0.0/12
            if (b[0] == 192 && b[1] == 168) return true;                // 192.168.0.0/16
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC) return true;                 // fc00::/7 ULA
        }
        return false;
    }
}
