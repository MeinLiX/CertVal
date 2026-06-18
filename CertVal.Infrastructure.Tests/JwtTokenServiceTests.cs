using CertVal.Core.Entities;
using CertVal.Infrastructure.Authentication;
using CertVal.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CertVal.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    // 64 chars -> 512 bits, comfortably above the HMAC-SHA256 minimum.
    private const string ValidKey = "CertVal-Test-Secret-Key-Which-Is-Definitely-Long-Enough-0123456789";

    private static IConfiguration Config(string? secretKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "CertVal",
            ["JwtSettings:Audience"] = "CertVal-Users",
            ["JwtSettings:AccessTokenExpiryMinutes"] = "15"
        };
        if (secretKey is not null) values["JwtSettings:SecretKey"] = secretKey;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static User TestUser()
        => User.Create("confirm@example.com", "hash", "Ada", "Lovelace");

    [Fact]
    public void GenerateToken_WithValidKey_ProducesValidatableToken()
    {
        var service = new JwtTokenService(Config(ValidKey));

        var token = service.GenerateToken(TestUser());

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(2, token.Count(c => c == '.')); // header.payload.signature
        Assert.True(service.ValidateTokenAsync(token).GetAwaiter().GetResult());
    }

    [Fact]
    public void GenerateToken_WithShortKey_ThrowsActionableError()
    {
        // This is the exact regression: "SuperSecretKey" is 14 bytes / 112 bits.
        var service = new JwtTokenService(Config("SuperSecretKey"));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken(TestUser()));
        Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetKeyBytes_WithMissingKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => JwtSigningKey.GetKeyBytes(Config(null)));
    }

    [Fact]
    public void GetKeyBytes_AtMinimumLength_Succeeds()
    {
        var key = new string('k', JwtSigningKey.MinKeyBytes);
        var bytes = JwtSigningKey.GetKeyBytes(Config(key));
        Assert.Equal(JwtSigningKey.MinKeyBytes, bytes.Length);
    }

    [Fact]
    public void GetKeyBytes_OneByteShort_Throws()
    {
        var key = new string('k', JwtSigningKey.MinKeyBytes - 1);
        Assert.Throws<InvalidOperationException>(() => JwtSigningKey.GetKeyBytes(Config(key)));
    }
}
