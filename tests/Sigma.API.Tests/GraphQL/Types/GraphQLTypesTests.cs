using Sigma.API.GraphQL;
using Xunit;

namespace Sigma.API.Tests.GraphQL.Types;

public class GraphQLTypesTests
{
    [Fact]
    public void CreateTenantInput_ShouldInitializeProperties()
    {
        // Arrange & Act
        var input = new CreateTenantInput(
            Name: "Test Tenant",
            Slug: "test-tenant",
            PlanType: "professional",
            RetentionDays: 30);

        // Assert
        Assert.Equal("Test Tenant", input.Name);
        Assert.Equal("test-tenant", input.Slug);
        Assert.Equal("professional", input.PlanType);
        Assert.Equal(30, input.RetentionDays);
    }

    [Fact]
    public void CreateTenantPayload_ShouldInitializeProperties()
    {
        // Arrange & Act
        var payload = new CreateTenantPayload
        {
            Success = true,
            Tenant = new Domain.Entities.Tenant("Test", "test", "free", 30)
        };

        // Assert
        Assert.True(payload.Success);
        Assert.NotNull(payload.Tenant);
    }

    [Fact]
    public void CreateWorkspaceInput_ShouldInitializeProperties()
    {
        // Arrange & Act
        var input = new CreateWorkspaceInput(
            TenantId: Guid.NewGuid(),
            Name: "Test Workspace",
            Platform: "slack",
            ExternalId: "ext-123");

        // Assert
        Assert.NotEqual(Guid.Empty, input.TenantId);
        Assert.Equal("Test Workspace", input.Name);
        Assert.Equal("slack", input.Platform);
        Assert.Equal("ext-123", input.ExternalId);
    }

    [Fact]
    public void CreateWorkspacePayload_ShouldInitializeProperties()
    {
        // Arrange & Act
        var tenantId = Guid.NewGuid();
        var workspace = new Domain.Entities.Workspace(tenantId, "Test Workspace", "slack");
        workspace.UpdateExternalId("ext-ws-123");

        var payload = new CreateWorkspacePayload
        {
            Success = true,
            Workspace = workspace
        };

        // Assert
        Assert.True(payload.Success);
        Assert.NotNull(payload.Workspace);
    }

    [Fact]
    public void CreateChannelInput_ShouldInitializeProperties()
    {
        // Arrange & Act
        var input = new CreateChannelInput(
            TenantId: Guid.NewGuid(),
            WorkspaceId: Guid.NewGuid(),
            Name: "Test Channel",
            ExternalId: "ch-123");

        // Assert
        Assert.NotEqual(Guid.Empty, input.TenantId);
        Assert.NotEqual(Guid.Empty, input.WorkspaceId);
        Assert.Equal("Test Channel", input.Name);
        Assert.Equal("ch-123", input.ExternalId);
    }

    [Fact]
    public void CreateChannelPayload_ShouldInitializeProperties()
    {
        // Arrange & Act
        var workspaceId = Guid.NewGuid();
        var payload = new CreateChannelPayload
        {
            Success = true,
            Channel = new Domain.Entities.Channel(workspaceId, "Test", "ch-123")
        };

        // Assert
        Assert.True(payload.Success);
        Assert.NotNull(payload.Channel);
    }

    [Fact]
    public void UpdateTenantPlanInput_ShouldInitializeProperties()
    {
        // Arrange & Act
        var input = new UpdateTenantPlanInput(
            TenantId: Guid.NewGuid(),
            PlanType: "enterprise",
            RetentionDays: 90);

        // Assert
        Assert.NotEqual(Guid.Empty, input.TenantId);
        Assert.Equal("enterprise", input.PlanType);
        Assert.Equal(90, input.RetentionDays);
    }

    [Fact]
    public void UpdateTenantPlanPayload_ShouldInitializeProperties()
    {
        // Arrange & Act
        var payload = new UpdateTenantPlanPayload
        {
            Success = true,
            Tenant = new Domain.Entities.Tenant("Test", "test", "professional", 60)
        };

        // Assert
        Assert.True(payload.Success);
        Assert.NotNull(payload.Tenant);
    }

    [Fact]
    public void UserError_ShouldInitializeProperties()
    {
        // Arrange & Act
        var error = new UserError(
            Message: "Test error",
            Code: "TEST_ERROR");

        // Assert
        Assert.Equal("Test error", error.Message);
        Assert.Equal("TEST_ERROR", error.Code);
    }

    [Fact]
    public void Payload_ShouldInitializeProperties()
    {
        // Arrange & Act
        var payload = new TestPayload
        {
            Success = true,
            Errors = null
        };

        // Assert
        Assert.True(payload.Success);
        Assert.Null(payload.Errors);
    }

    [Fact]
    public void Payload_WithErrors_ShouldNotBeSuccess()
    {
        // Arrange & Act
        var payload = new TestPayload
        {
            Success = false,
            Errors = new[] { new UserError("Error", "ERROR") }
        };

        // Assert
        Assert.False(payload.Success);
        Assert.NotNull(payload.Errors);
        Assert.Single(payload.Errors);
    }

    private class TestPayload : Payload { }
}