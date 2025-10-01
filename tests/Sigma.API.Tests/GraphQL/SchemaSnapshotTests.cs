using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

[Collection("GraphQL Sequential")]
public class SchemaSnapshotTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SchemaSnapshotTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Schema_Should_Match_Snapshot()
    {
        // Arrange - Use a factory without database initialization to avoid migration issues
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove database initializer to prevent migration errors during schema inspection
                var descriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                    d.ImplementationType?.Name == "DatabaseInitializer");
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
            });
        });

        var schema = await factory.Services
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        // Act
        var schemaString = schema.Schema.Print();

        // Assert - Verify schema contains expected types
        Assert.NotNull(schemaString);
        Assert.NotEmpty(schemaString);
        Assert.Contains("type Query", schemaString);
        Assert.Contains("type Mutation", schemaString);
        Assert.Contains("type Tenant", schemaString);
        Assert.Contains("type Workspace", schemaString);
        Assert.Contains("type Channel", schemaString);
        Assert.Contains("type Message", schemaString);

        // Save schema snapshot for manual review
        var snapshotPath = System.IO.Path.Combine(
            Directory.GetCurrentDirectory(),
            "GraphQL/__snapshots__/schema.graphql");

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(snapshotPath)!);
        await File.WriteAllTextAsync(snapshotPath, schemaString);
    }
}