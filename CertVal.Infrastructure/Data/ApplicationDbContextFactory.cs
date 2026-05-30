using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CertVal.Infrastructure.Data;

/// <summary>
/// Design-time factory used only by the EF Core CLI (e.g. `dotnet ef migrations add`).
/// It is never used at runtime; the application configures the context via Aspire.
/// The connection string is a placeholder because schema operations do not require a live database.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=certval_design;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
