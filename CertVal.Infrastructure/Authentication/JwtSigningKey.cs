using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CertVal.Infrastructure.Authentication;

/// <summary>
/// Centralizes resolution of the symmetric JWT signing key so token generation
/// and validation always agree on the bytes and encoding, and so a misconfigured
/// (too short) key fails fast with an actionable message instead of throwing a
/// cryptic <c>IDX10653</c> deep inside token creation.
/// </summary>
public static class JwtSigningKey
{
    // HMAC-SHA256 keys must be at least the hash size (256 bits / 32 bytes) per
    // RFC 7518 §3.2. The IdentityModel library hard-fails below 128 bits.
    public const int MinKeyBytes = 32;

    public static byte[] GetKeyBytes(IConfiguration configuration)
    {
        var secretKey = configuration.GetSection("JwtSettings")["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT SecretKey not configured (JwtSettings:SecretKey).");

        var bytes = Encoding.UTF8.GetBytes(secretKey);
        if (bytes.Length < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"JWT SecretKey is too short: {bytes.Length * 8} bits. " +
                $"HMAC-SHA256 requires a key of at least {MinKeyBytes * 8} bits " +
                $"({MinKeyBytes} bytes / characters). Configure a longer JwtSettings:SecretKey.");
        }

        return bytes;
    }

    public static SymmetricSecurityKey GetSecurityKey(IConfiguration configuration)
        => new(GetKeyBytes(configuration));
}
