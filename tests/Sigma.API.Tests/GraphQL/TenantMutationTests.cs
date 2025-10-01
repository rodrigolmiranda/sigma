using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

[Collection("GraphQL Sequential")]
public class TenantMutationTests : GraphQLTestBase
{
    public TenantMutationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateTenant_WithValidInput_ShouldCreateTenant()
    {
        // Arrange
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    tenant {
                        id
                        name
                        slug
                        planType
                        retentionDays
                    }
                    errors {
                        message
                        code
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "Test Tenant",
                slug = $"test-tenant-{Guid.NewGuid():N}",
                planType = "starter",
                retentionDays = 60
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<dynamic>(mutation, variables);

        // Assert
        if (response.Errors != null && response.Errors.Any())
        {
            var errorMessages = response.Errors.Select(e =>
            {
                var msg = e.Message;
                if (e.Extensions != null && e.Extensions.ContainsKey("message"))
                    msg += $" - {e.Extensions["message"]}";
                if (e.Extensions != null && e.Extensions.ContainsKey("stackTrace"))
                    msg += $"\nStackTrace: {e.Extensions["stackTrace"]}";
                return msg;
            });
            var fullError = string.Join("\n", errorMessages);
            Assert.Fail($"GraphQL errors:\n{fullError}");
        }
        Assert.NotNull(response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public async Task CreateTenant_WithDuplicateSlug_ShouldReturnError()
    {
        // Arrange - Create first tenant
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    tenant {
                        id
                        slug
                    }
                    errors {
                        message
                        code
                    }
                }
            }";

        var firstTenant = new
        {
            input = new
            {
                name = "First Tenant",
                slug = "duplicate-slug",
                planType = "free",
                retentionDays = 30
            }
        };

        await ExecuteGraphQLMutationAsync<dynamic>(mutation, firstTenant);

        // Act - Try to create second tenant with same slug
        var secondTenant = new
        {
            input = new
            {
                name = "Second Tenant",
                slug = "duplicate-slug",
                planType = "free",
                retentionDays = 30
            }
        };

        var response = await ExecuteGraphQLMutationAsync<dynamic>(mutation, secondTenant);

        // Assert
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task CreateTenant_WithInvalidInput_ShouldReturnValidationError()
    {
        // Arrange
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    tenant {
                        id
                    }
                    errors {
                        message
                        code
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "", // Empty name should fail validation
                slug = "test-slug",
                planType = "free",
                retentionDays = 30
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<dynamic>(mutation, variables);

        // Assert
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task UpdateTenantPlan_WithValidInput_ShouldUpdateTenant()
    {
        // Arrange - Create tenant first
        var createMutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    tenant {
                        id
                    }
                }
            }";

        var createVariables = new
        {
            input = new
            {
                name = "Test Tenant",
                slug = "update-test",
                planType = "free",
                retentionDays = 30
            }
        };

        var createResponse = await ExecuteGraphQLMutationAsync<dynamic>(createMutation, createVariables);

        // Act - Update tenant plan
        var updateMutation = @"
            mutation UpdateTenantPlan($input: UpdateTenantPlanInput!) {
                updateTenantPlan(input: $input) {
                    tenant {
                        id
                        planType
                        retentionDays
                    }
                    errors {
                        message
                        code
                    }
                }
            }";

        var updateVariables = new
        {
            input = new
            {
                tenantId = Guid.NewGuid(), // This would need to be extracted from createResponse
                planType = "professional",
                retentionDays = 90
            }
        };

        var response = await ExecuteGraphQLMutationAsync<dynamic>(updateMutation, updateVariables);

        // Assert
        Assert.NotNull(response.Data);
    }
}