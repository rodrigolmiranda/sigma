using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

[Collection("GraphQL Sequential")]
public class TenantQueryTests : GraphQLTestBase
{
    public TenantQueryTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTenant_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange - Add a tenant to the database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();
            var tenant = new Tenant("Test Tenant", $"test-tenant-query-{Guid.NewGuid():N}", "free", 30);
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();

            // Act
            var query = @"
                query GetTenant($id: UUID!) {
                    tenant(id: $id) {
                        id
                        name
                        slug
                        planType
                        retentionDays
                    }
                }";

            var variables = new
            {
                id = tenant.Id
            };

            var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);

            // Assert
            Assert.NotNull(response.Data);
            Assert.Null(response.Errors);
        }
    }

    [Fact]
    public async Task GetTenant_WithNonExistentTenant_ShouldReturnNull()
    {
        // Arrange
        var query = @"
            query GetTenant($id: UUID!) {
                tenant(id: $id) {
                    id
                    name
                }
            }";

        var variables = new
        {
            id = Guid.NewGuid()
        };

        // Act
        var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);

        // Assert
        Assert.NotNull(response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public async Task GetTenants_WithMultipleTenants_ShouldReturnAll()
    {
        // Arrange - Add multiple tenants
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

            var tenant1 = new Tenant("Tenant 1", $"tenant-1-{Guid.NewGuid():N}", "free", 30);
            var tenant2 = new Tenant("Tenant 2", $"tenant-2-{Guid.NewGuid():N}", "starter", 60);
            var tenant3 = new Tenant("Tenant 3", $"tenant-3-{Guid.NewGuid():N}", "professional", 90);

            dbContext.Tenants.AddRange(tenant1, tenant2, tenant3);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var query = @"
            query GetTenants {
                tenants {
                    id
                    name
                    slug
                    planType
                }
            }";

        var response = await ExecuteGraphQLQueryAsync<dynamic>(query);

        // Assert
        Assert.NotNull(response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public async Task GetWorkspaces_ForTenant_ShouldReturnWorkspaces()
    {
        // Arrange - Add tenant with workspaces
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

            var tenant = new Tenant("Tenant with Workspaces", $"tenant-workspaces-{Guid.NewGuid():N}", "free", 30);
            var workspace1 = tenant.AddWorkspace("Workspace 1", Platform.Slack);
            var workspace2 = tenant.AddWorkspace("Workspace 2", Platform.Discord);

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();

            // Act
            var query = @"
                query GetTenant($id: UUID!) {
                    tenant(id: $id) {
                        id
                        name
                        workspaces {
                            id
                            name
                            platform
                        }
                    }
                }";

            var variables = new
            {
                id = tenant.Id
            };

            var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);

            // Assert
            Assert.NotNull(response.Data);
            Assert.Null(response.Errors);
        }
    }
}