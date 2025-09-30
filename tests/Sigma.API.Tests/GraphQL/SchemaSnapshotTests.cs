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

    [Fact(Skip = "EF Core pending migrations warning - tooling issue with .NET 10 RC1 on macOS (path separator bug)")]
    public async Task Schema_Should_Match_Snapshot()
    {
        // Arrange
        var schema = await _factory.Services
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        // Act
        var schemaString = schema.Schema.Print();

        // Assert - For now just verify schema is not empty
        Assert.NotNull(schemaString);
        Assert.NotEmpty(schemaString);
        Assert.Contains("type Query", schemaString);
        Assert.Contains("type Mutation", schemaString);

        // TODO: Add proper snapshot testing with Verify or similar library
        // For now, save the schema to a file for manual verification
        var snapshotPath = System.IO.Path.Combine(
            Directory.GetCurrentDirectory(),
            "GraphQL/__snapshots__/schema.graphql");

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(snapshotPath)!);
        await File.WriteAllTextAsync(snapshotPath, schemaString);
    }
}