using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("CertVal-database")
                ?? throw new InvalidOperationException("Database connection string 'CertVal-database' not found.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

            if (configuration.GetValue<bool>("DetailedErrors"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            if (configuration.GetValue<bool>("Logging:Database:EnableQueryLogging"))
            {
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IApiTokenRepository, ApiTokenRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Starting database migration...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");

            // Seed data in development
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                logger.LogInformation("Seeding development data...");
                await context.SeedDevelopmentDataAsync();
                logger.LogInformation("Development data seeding completed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);
            throw;
        }
    }
}

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(this ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var defaultUser = User.Create(
                "admin@certval.dev",
                BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                "Admin",
                "User"
            );
            defaultUser.ConfirmEmail();

            await context.Users.AddAsync(defaultUser);
            await context.SaveChangesAsync();

            var demoUser = User.Create(
                "demo@certval.dev",
                BCrypt.Net.BCrypt.HashPassword("Demo123!"),
                "Demo",
                "User"
            );
            demoUser.ConfirmEmail();

            await context.Users.AddAsync(demoUser);
            await context.SaveChangesAsync();

            var defaultWorkspace = Workspace.Create(
                "Production Environment",
                defaultUser.Id,
                "Main production workspace for certificate monitoring"
            );

            await context.Workspaces.AddAsync(defaultWorkspace);

            var demoWorkspace = Workspace.Create(
                "Demo Workspace",
                demoUser.Id,
                "Demo workspace for testing"
            );

            await context.Workspaces.AddAsync(demoWorkspace);
            await context.SaveChangesAsync();

            // Create notification rules for default workspace
            var notificationRules = new[]
            {
                NotificationRule.Create(
                    defaultWorkspace.Id,
                    "Critical - 7 days",
                    7,
                    Core.Enums.NotificationChannelType.Email,
                    $"{{\"email\":\"{defaultUser.Email}\",\"priority\":\"high\"}}"
                ),
                NotificationRule.Create(
                    defaultWorkspace.Id,
                    "Warning - 30 days",
                    30,
                    Core.Enums.NotificationChannelType.Email,
                    $"{{\"email\":\"{defaultUser.Email}\",\"priority\":\"normal\"}}"
                ),
                NotificationRule.Create(
                    defaultWorkspace.Id,
                    "Info - 60 days",
                    60,
                    Core.Enums.NotificationChannelType.Email,
                    $"{{\"email\":\"{defaultUser.Email}\",\"priority\":\"low\"}}"
                )
            };

            await context.NotificationRules.AddRangeAsync(notificationRules);

            // Create notification rules for demo workspace
            var demoNotificationRule = NotificationRule.Create(
                demoWorkspace.Id,
                "Demo notifications",
                15,
                Core.Enums.NotificationChannelType.Email,
                $"{{\"email\":\"{demoUser.Email}\"}}"
            );

            await context.NotificationRules.AddAsync(demoNotificationRule);

            var workspaceMember = WorkspaceMember.Create(
                defaultWorkspace.Id,
                demoUser.Id,
                Core.Enums.WorkspaceRole.Viewer,
                defaultUser.Id
            );
            workspaceMember.AcceptInvitation();

            await context.WorkspaceMembers.AddAsync(workspaceMember);

            await SeedSampleCertificatesAsync(context, defaultWorkspace.Id, demoWorkspace.Id);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task SeedSampleCertificatesAsync(ApplicationDbContext context, Guid defaultWorkspaceId, Guid demoWorkspaceId)
    {
        var sampleCertificates = new[]
        {
            Certificate.Create(
                defaultWorkspaceId,
                "CN=api.certval.dev, O=CertVal",
                "CN=Let's Encrypt Authority X3, O=Let's Encrypt, C=US",
                "1234567890ABCDEF1234567890ABCDEF12345678",
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(60), // Expires in 60 days
                "api-certval-dev.crt",
                "/certificates/api-certval-dev.crt",
                Core.Enums.CertificateFormat.CRT,
                2048,
                "12345"
            ),
            Certificate.Create(
                defaultWorkspaceId,
                "CN=www.certval.dev, O=CertVal",
                "CN=Let's Encrypt Authority X3, O=Let's Encrypt, C=US",
                "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
                DateTime.UtcNow.AddDays(-60),
                DateTime.UtcNow.AddDays(30), // Expires in 30 days
                "www-certval-dev.crt",
                "/certificates/www-certval-dev.crt",
                Core.Enums.CertificateFormat.CRT,
                2048,
                "67890"
            ),
            Certificate.Create(
                defaultWorkspaceId,
                "CN=old.certval.dev, O=CertVal",
                "CN=Let's Encrypt Authority X3, O=Let's Encrypt, C=US",
                "FEDCBA0987654321FEDCBA0987654321FEDCBA09",
                DateTime.UtcNow.AddDays(-95),
                DateTime.UtcNow.AddDays(5), // Expires in 5 days - critical
                "old-certval-dev.crt",
                "/certificates/old-certval-dev.crt",
                Core.Enums.CertificateFormat.CRT,
                2048,
                "11111"
            ),
            Certificate.Create(
                demoWorkspaceId,
                "CN=demo.example.com, O=Demo Corp",
                "CN=DigiCert SHA2 Secure Server CA, O=DigiCert Inc, C=US",
                "9876543210FEDCBA9876543210FEDCBA98765432",
                DateTime.UtcNow.AddDays(-180),
                DateTime.UtcNow.AddDays(90), // Expires in 90 days
                "demo-example-com.crt",
                "/certificates/demo-example-com.crt",
                Core.Enums.CertificateFormat.CRT,
                4096,
                "DEMO1"
            ),
            Certificate.Create(
                demoWorkspaceId,
                "CN=expired.example.com, O=Demo Corp",
                "CN=DigiCert SHA2 Secure Server CA, O=DigiCert Inc, C=US",
                "1111111111111111111111111111111111111111",
                DateTime.UtcNow.AddDays(-400),
                DateTime.UtcNow.AddDays(-10), // Already expired
                "expired-example-com.crt",
                "/certificates/expired-example-com.crt",
                Core.Enums.CertificateFormat.CRT,
                2048,
                "EXP01"
            )
        };

        sampleCertificates[4].MarkAsExpired();

        await context.Certificates.AddRangeAsync(sampleCertificates);
    }
}