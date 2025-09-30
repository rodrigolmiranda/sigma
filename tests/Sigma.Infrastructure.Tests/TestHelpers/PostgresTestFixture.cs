using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Sigma.Infrastructure.Tests.TestHelpers;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public PostgresTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("sigma_test")
            .WithUsername("sigma_test")
            .WithPassword("sigma_test123")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Create the database schema
        using var scope = CreateServiceScope();
        var context = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    public IServiceScope CreateServiceScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SigmaDbContext>(options =>
            options.UseNpgsql(ConnectionString));

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.CreateScope();
    }

    public DbContextOptions<SigmaDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<SigmaDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }
}