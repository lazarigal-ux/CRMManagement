using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRMManagement.Infrastructure.Data;

/// <summary>
/// Used by `dotnet ef` so migrations can be generated without spinning up the full Web host
/// (which can be blocked by file locks on Windows when the app is running). Connection
/// values come from environment variables; defaults match a local dev Postgres.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var db   = Environment.GetEnvironmentVariable("DB_NAME") ?? "ldatabrain";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

        var cs = $"Host={host};Port={port};Database={db};Username={user};Password={pass};Search Path=crm,public";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("crm___EFMigrationsHistory", "crm");
            })
            .Options;

        return new AppDbContext(options);
    }
}
