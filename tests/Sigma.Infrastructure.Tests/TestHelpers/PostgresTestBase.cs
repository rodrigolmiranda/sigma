using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Infrastructure.Persistence;
using Xunit;

namespace Sigma.Infrastructure.Tests.TestHelpers;

public abstract class PostgresTestBase : IAsyncLifetime
{
    private static readonly string ConnectionString = "Host=localhost;Port=5433;Database=sigma_test;Username=sigma_test;Password=sigma_test123;Include Error Detail=true";
    private string _schemaName;
    protected SigmaDbContext Context { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    protected PostgresTestBase()
    {
        // Use a unique schema for each test to ensure isolation
        _schemaName = $"test_{Guid.NewGuid():N}";
    }

    public virtual async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SigmaDbContext>(options =>
            options.UseNpgsql(ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", _schemaName);
            }));

        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<SigmaDbContext>();

        // Create the schema - schema name is a GUID, SQL injection not a concern
#pragma warning disable EF1002
        await Context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {_schemaName}");

        // Set the search path to use our test schema
        await Context.Database.ExecuteSqlRawAsync($"SET search_path TO {_schemaName}");
#pragma warning restore EF1002

        // Drop and recreate tables to ensure schema is up to date
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up the test schema
        if (Context != null)
        {
            try
            {
                // Only try to drop schema if context is not disposed - schema name is a GUID
#pragma warning disable EF1002
                await Context.Database.ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS {_schemaName} CASCADE");
#pragma warning restore EF1002
            }
            catch (ObjectDisposedException)
            {
                // Context was already disposed, skip cleanup
            }

            try
            {
                await Context.DisposeAsync();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
        }

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    protected DbContextOptions<SigmaDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<SigmaDbContext>()
            .UseNpgsql(ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", _schemaName);
            })
            .Options;
    }
}