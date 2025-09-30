using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sigma.Infrastructure.Persistence;

public class SigmaDbContextFactory : IDesignTimeDbContextFactory<SigmaDbContext>
{
    public SigmaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SigmaDbContext>();

        // Use a default connection string for design-time operations
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=sigma;Username=sigma;Password=sigma123";

        optionsBuilder.UseNpgsql(connectionString);

        return new SigmaDbContext(optionsBuilder.Options);
    }
}