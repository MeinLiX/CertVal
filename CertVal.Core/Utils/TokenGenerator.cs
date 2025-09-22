using System.Security.Cryptography;
using System.Text;

namespace CertVal.Core.Utils;

public static class TokenGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int MinLength = 16;
    private const int MaxLength = 256;

    public static string GenerateUrlSafeToken(int length = 32)
    {
        if (length < MinLength || length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length),
                $"Token length must be between {MinLength} and {MaxLength}");
        }

        var token = new char[length];
        var charsetLength = Chars.Length;
        var maxUsableValue = byte.MaxValue - (byte.MaxValue % charsetLength);

        using var rng = RandomNumberGenerator.Create();
        var position = 0;

        while (position < length)
        {
            var bytes = new byte[(length - position) * 2];
            rng.GetBytes(bytes);

            foreach (var b in bytes)
            {
                if (b < maxUsableValue)
                {
                    token[position] = Chars[b % charsetLength];
                    position++;

                    if (position >= length)
                        break;
                }
            }
        }

        return new string(token);
    }

    public static (string token, string hash) GenerateApiToken(int byteLength = 32)
    {
        if (byteLength < 16 || byteLength > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength),
                "Byte length should be between 16 and 64");
        }

        Span<byte> tokenBytes = byteLength <= 48
            ? stackalloc byte[byteLength]
            : new byte[byteLength];

        RandomNumberGenerator.Fill(tokenBytes);

        var token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var hash = HashApiToken(token);

        tokenBytes.Clear();

        return (token, hash);
    }

    public static string HashApiToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}