using CertVal.Application;
using CertVal.Application.Common.Interfaces;
using CertVal.Application.Middleware;
using CertVal.Application.Services;
using CertVal.Infrastructure;
using CertVal.Infrastructure.Authentication;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/home/app/dataprotection-keys"));

builder.AddRabbitMQClient("CertVal-rabbitmq");

builder.AddRedisClient("CertVal-redis");

builder.AddMinioClient("CertVal-minio");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new CertVal.Infrastructure.Converters.FlexibleEnumConverterFactory());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "CertVal API",
            Version = "v1",
            Description = "Certificate monitoring and management API",
            Contact = new() { Name = "CertVal Support", Email = "support@certval.halerka.dev" }
        };

        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            },
            ["ApiKey"] = new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key for programmatic access"
            }
        };

        document.Security =
        [
            new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] },
            new() { [new OpenApiSecuritySchemeReference("ApiKey", document)] = [] }
        ];

        return Task.CompletedTask;
    });
});

builder.Services.AddHttpContextAccessor();

builder.AddNpgsqlDbContext<ApplicationDbContext>(
    connectionName: "CertVal-database",
    configureDbContextOptions: options =>
    {
        DatabaseConfiguration.ConfigureApplicationDbContext(options, builder.Configuration);
    }
);

builder.Services.AddInfrastructure();

// Register application services
builder.Services.AddApplicationServices();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICertificateProcessorService, CertificateProcessorService>();

builder.Services.AddCustomAuthentication(builder.Configuration);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireClaim("api_scope", "Admin"))
    .AddPolicy("WriteAccess", policy => policy.RequireAssertion(context =>
        context.User.HasClaim("api_scope", "ReadWrite") ||
        context.User.HasClaim("api_scope", "Admin") ||
        context.User.HasClaim("client_type", "web")))
    .AddPolicy("ReadAccess", policy => policy.RequireAuthenticatedUser());

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? throw new Exception("Cors:AllowedOrigins not configured")
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
});

// Register background services
builder.Services.AddSingleton<CertificateExpiryCheckerService>();
builder.Services.AddSingleton<ICertificateExpiryChecker>(sp => sp.GetRequiredService<CertificateExpiryCheckerService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<CertificateExpiryCheckerService>());

builder.Services.AddHostedService<CertificateStorageInitializationService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapOpenApi("/api/openapi/{documentName}.json");
app.MapScalarApiReference("/api/scalar", options =>
{
    options.WithTitle("CertVal API Documentation");
    options.WithTheme(ScalarTheme.Default);
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.WithOpenApiRoutePattern("/api/openapi/{documentName}.json");
    options.Servers = Array.Empty<ScalarServer>();
});

app.MapGet("/", () => Results.Redirect("/api/scalar/v1"))
    .ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    // Development-only endpoints
    var devGroup = app.MapGroup("dev").WithTags("Development");

    devGroup.MapPost("/certificate-expiry/check-now", async (ICertificateExpiryChecker checker, CancellationToken ct) =>
    {
        await checker.TriggerCheckNowAsync(ct);
        return Results.Accepted(value: new { message = "Triggered certificate expiry check" });
    })
    .WithName("TriggerCertificateExpiryCheckNow")
    .WithDescription("Immediately triggers the certificate expiry checker to run and resets its delay.")
    .WithTags("Development");
}

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapDefaultEndpoints();

try
{
    await app.Services.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    try
    {
        app.Logger.LogCritical(ex, "Application failed to start");
    }
    catch
    {
        Console.Error.WriteLine($"Application failed to start: {ex}");
    }
    throw;
}