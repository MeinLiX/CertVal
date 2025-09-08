using CertVal.Core.Entities;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CertVal.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("/FILL/"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(30);
                });

            // Enable sensitive data logging only in development
            if (configuration.GetValue<bool>("DetailedErrors"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IApiTokenRepository, ApiTokenRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("/FILL/");

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            // Log the exception - in production you'd want proper logging
            Console.WriteLine($"Database migration failed: {ex.Message}");
            throw;
        }
    }
}

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(this ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var defaultUser = User.Create(
            "defus@certval.dev",
            BCrypt.Net.BCrypt.HashPassword("Password123!"),
            "Default",
            "User"
        );
        defaultUser.ConfirmEmail();

        await context.Users.AddAsync(defaultUser);
        await context.SaveChangesAsync();

        var defaultWorkspace = Workspace.Create(
            "Default Workspace",
            "Your default workspace for certificate monitoring",
            defaultUser.Id
        );

        await context.Workspaces.AddAsync(defaultWorkspace);

        var emailRule = NotificationRule.Create(
            defaultWorkspace.Id,
            "Email - 30 days before expiry",
            30,
            Core.Enums.NotificationChannelType.Email,
            $"{{\"email\":\"{defaultUser.Email}\"}}"
        );

        var weeklyEmailRule = NotificationRule.Create(
            defaultWorkspace.Id,
            "Email - 7 days before expiry",
            7,
            Core.Enums.NotificationChannelType.Email,
            $"{{\"email\":\"{defaultUser.Email}\"}}"
        );

        await context.NotificationRules.AddRangeAsync(emailRule, weeklyEmailRule);
        await context.SaveChangesAsync();
    }
}