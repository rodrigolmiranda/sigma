using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Sigma.Infrastructure.Persistence;
using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

public class GraphQLTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _baseFactory;
    protected WebApplicationFactory<Program> _factory = null!;
    protected HttpClient _client = null!;
    private string _schemaName = null!;
    private readonly string _connectionString = "Host=localhost;Port=5433;Database=sigma_test;Username=sigma_test;Password=sigma_test123;Include Error Detail=true";

    public GraphQLTestBase(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    public async ValueTask InitializeAsync()
    {
        // Generate unique schema name for THIS test execution
        _schemaName = $"test_{Guid.NewGuid():N}";

        // Configure factory with this test's unique schema
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing database-related registrations including pools
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<SigmaDbContext>) ||
                               d.ServiceType == typeof(DbContextOptions) ||
                               d.ServiceType == typeof(IDbContextFactory<SigmaDbContext>) ||
                               d.ServiceType == typeof(SigmaDbContext) ||
#pragma warning disable EF1001 // Internal EF API usage acceptable for test isolation
                               d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<>) ||
#pragma warning restore EF1001
                               (d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                                d.ImplementationType == typeof(Sigma.Infrastructure.Persistence.DatabaseInitializer)))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add PostgreSQL DbContext using NpgsqlDataSource with connection initialization
                // This is the CORRECT way to ensure search_path is set on EVERY connection
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);

                // Configure connection initialization - this runs on EVERY connection opened from the pool
                dataSourceBuilder.UsePhysicalConnectionInitializer(
                    connectionInitializer: conn =>
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"SET search_path TO {_schemaName}";
                        cmd.ExecuteNonQuery();
                    },
                    connectionInitializerAsync: async conn =>
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"SET search_path TO {_schemaName}";
                        await cmd.ExecuteNonQueryAsync();
                    }
                );

                var dataSource = dataSourceBuilder.Build();

                // Register the data source as singleton
                services.AddSingleton(dataSource);

                // Add pooled DbContext factory - this provides both IDbContextFactory and scoped DbContext
                services.AddPooledDbContextFactory<SigmaDbContext>(options =>
                {
                    options.UseNpgsql(dataSource)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                });

                // Disable authorization for tests
                services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AllowAnonymousAuthorizationHandler>();
            });
        });

        // Create HTTP client for this test
        _client = _factory.CreateClient();

        // Create test schema
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var createSchemaCommand = connection.CreateCommand();
        createSchemaCommand.CommandText = $"CREATE SCHEMA IF NOT EXISTS {_schemaName}";
        await createSchemaCommand.ExecuteNonQueryAsync();

        // Ensure database is created in the test schema
        // Get DbContext from the pooled factory
        var factory = _factory.Services.GetRequiredService<IDbContextFactory<SigmaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        // Explicitly set search_path for this connection (double-check it's set)
        // Schema name is a GUID generated in InitializeAsync, so SQL injection is not a concern
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync($"SET search_path TO {_schemaName}");
#pragma warning restore EF1002

        // Create tables in the test schema
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up the test schema
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS {_schemaName} CASCADE";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    protected async Task<GraphQLResponse<T>> ExecuteGraphQLQueryAsync<T>(string query, object? variables = null)
    {
        var graphQLRequest = new
        {
            query,
            variables
        };

        var content = new StringContent(
            JsonSerializer.Serialize(graphQLRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/graphql", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseContent, options);
        return result ?? throw new InvalidOperationException("Failed to deserialize GraphQL response");
    }

    protected async Task<GraphQLResponse<T>> ExecuteGraphQLMutationAsync<T>(string mutation, object? variables = null)
    {
        return await ExecuteGraphQLQueryAsync<T>(mutation, variables);
    }

    protected async Task SeedDataAsync(Action<SigmaDbContext> seedAction)
    {
        // Use the factory to create DbContext - this ensures connection initializer is applied
        var factory = _factory.Services.GetRequiredService<IDbContextFactory<SigmaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        // Connection initializer automatically sets search_path for all connections
        seedAction(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<T> WithDbContextAsync<T>(Func<SigmaDbContext, Task<T>> action)
    {
        // Use the factory to create DbContext - this ensures connection initializer is applied
        var factory = _factory.Services.GetRequiredService<IDbContextFactory<SigmaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        return await action(dbContext);
    }

    protected async Task WithDbContextAsync(Func<SigmaDbContext, Task> action)
    {
        // Use the factory to create DbContext - this ensures connection initializer is applied
        var factory = _factory.Services.GetRequiredService<IDbContextFactory<SigmaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        await action(dbContext);
    }

    protected class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public List<GraphQLError>? Errors { get; set; }
    }

    protected class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
        public List<string>? Path { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
    }

    private class AllowAnonymousAuthorizationHandler : Microsoft.AspNetCore.Authorization.IAuthorizationHandler
    {
        public Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context)
        {
            foreach (var requirement in context.PendingRequirements.ToList())
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        // Legacy disposal - now handled by DisposeAsync
    }
}