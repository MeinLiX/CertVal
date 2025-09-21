using CertVal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure;

public static class DatabaseConfiguration
{
    public static void ConfigureApplicationDbContext(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CertVal-database")
            ?? throw new InvalidOperationException("Database connection string 'CertVal-database' not found.");

        options.UseNpgsql(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

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
    }
}