using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CertVal.Infrastructure.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var securityKey = JwtSigningKey.GetSecurityKey(configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Smart";
            options.DefaultChallengeScheme = "Smart";
        })
        .AddPolicyScheme("Smart", "Authorization Bearer or ApiKey", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authorizationHeader?.StartsWith("Bearer ") == true)
                    return JwtBearerDefaults.AuthenticationScheme;

                if (context.Request.Headers.ContainsKey("X-API-Key"))
                    return ApiKeyAuthenticationOptions.DefaultScheme;

                // Default to JWT for other cases
                return JwtBearerDefaults.AuthenticationScheme;
            };
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationOptions.DefaultScheme,
            options => { });

        return services;
    }
}