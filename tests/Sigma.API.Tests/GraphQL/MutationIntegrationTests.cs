using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

[Collection("GraphQL Sequential")]
public class MutationIntegrationTests : GraphQLTestBase
{
    public MutationIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region CreateTenant Tests

    [Fact]
    public async Task CreateTenant_WithValidInput_ShouldCreateTenant()
    {
        // Arrange
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                        name
                        slug
                        planType
                        retentionDays
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "New Test Tenant",
                slug = $"new-test-tenant-{Guid.NewGuid():N}",
                planType = "professional",
                retentionDays = 60
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateTenantResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Errors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTenant);
        if (!response.Data.CreateTenant.Success)
        {
            var errors = response.Data.CreateTenant.Errors?.Select(e => $"{e.Code}: {e.Message}") ?? new[] { "Unknown error" };
            Assert.True(response.Data.CreateTenant.Success, $"Mutation failed with errors: {string.Join(", ", errors)}");
        }
        Assert.True(response.Data.CreateTenant.Success);
        Assert.Null(response.Data.CreateTenant.Errors);
        Assert.NotNull(response.Data.CreateTenant.Tenant);
        Assert.Equal("New Test Tenant", response.Data.CreateTenant.Tenant.Name);
        Assert.StartsWith("new-test-tenant-", response.Data.CreateTenant.Tenant.Slug);
        Assert.Equal("professional", response.Data.CreateTenant.Tenant.PlanType);
        Assert.Equal(60, response.Data.CreateTenant.Tenant.RetentionDays);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Slug.StartsWith("new-test-tenant-"));
        Assert.NotNull(tenant);
        Assert.Equal("New Test Tenant", tenant!.Name);
    }

    [Fact]
    public async Task CreateTenant_WithDuplicateSlug_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var uniqueSlug = $"existing-slug-{Guid.NewGuid():N}";
        var existingTenant = new Tenant("Existing Tenant", uniqueSlug, "starter", 30);

        dbContext.Tenants.Add(existingTenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "Another Tenant",
                slug = uniqueSlug,
                planType = "starter",
                retentionDays = 30
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateTenantResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTenant);
        Assert.False(response.Data.CreateTenant.Success);
        Assert.NotNull(response.Data.CreateTenant.Errors);
        Assert.NotEmpty(response.Data.CreateTenant.Errors);
        Assert.Null(response.Data.CreateTenant.Tenant);
    }

    [Fact]
    public async Task CreateTenant_WithInvalidName_ShouldReturnValidationError()
    {
        // Arrange
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "", // Empty name
                slug = "empty-name-tenant",
                planType = "starter",
                retentionDays = 30
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateTenantResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTenant);
        Assert.False(response.Data.CreateTenant.Success);
        Assert.NotNull(response.Data.CreateTenant.Errors);
        Assert.NotEmpty(response.Data.CreateTenant.Errors);
        Assert.Contains(response.Data.CreateTenant.Errors, e => e.Code == "VALIDATION_ERROR");
        Assert.Null(response.Data.CreateTenant.Tenant);
    }

    [Fact]
    public async Task CreateTenant_WithDefaultPlanType_ShouldUseFreePlan()
    {
        // Arrange
        var mutation = @"
            mutation CreateTenant($input: CreateTenantInput!) {
                createTenant(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        planType
                        retentionDays
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                name = "Default Plan Tenant",
                slug = $"tenant-{Guid.NewGuid():N}" // 39 chars, within 50 char limit
                // No planType or retentionDays specified
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateTenantResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Errors);
        Assert.NotNull(response.Data);

        // Debug: Check errors if success is false
        if (!response.Data.CreateTenant.Success && response.Data.CreateTenant.Errors != null)
        {
            var errorMessages = string.Join(", ", response.Data.CreateTenant.Errors.Select(e => $"{e.Code}: {e.Message}"));
            throw new Exception($"Mutation failed with errors: {errorMessages}");
        }

        Assert.True(response.Data.CreateTenant.Success);
        Assert.NotNull(response.Data.CreateTenant.Tenant);
        Assert.Equal("free", response.Data.CreateTenant.Tenant.PlanType);
        Assert.Equal(30, response.Data.CreateTenant.Tenant.RetentionDays); // Default retention
    }

    #endregion

    #region UpdateTenantPlan Tests

    [Fact]
    public async Task UpdateTenantPlan_WithValidInput_ShouldUpdatePlan()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant = new Tenant("Test Tenant", $"test-tenant-update-plan-{Guid.NewGuid():N}", "free", 30);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation UpdateTenantPlan($input: UpdateTenantPlanInput!) {
                updateTenantPlan(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                        planType
                        retentionDays
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                planType = "professional",
                retentionDays = 90
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<UpdateTenantPlanResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Errors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTenantPlan);
        Assert.True(response.Data.UpdateTenantPlan.Success);
        Assert.Null(response.Data.UpdateTenantPlan.Errors);
        Assert.NotNull(response.Data.UpdateTenantPlan.Tenant);
        Assert.Equal("professional", response.Data.UpdateTenantPlan.Tenant.PlanType);
        Assert.Equal(90, response.Data.UpdateTenantPlan.Tenant.RetentionDays);

        // Verify in database
        dbContext.Entry(tenant).Reload();
        Assert.Equal("professional", tenant.PlanType);
        Assert.Equal(90, tenant.RetentionDays);
    }

    [Fact]
    public async Task UpdateTenantPlan_WithNonExistentTenant_ShouldReturnError()
    {
        // Arrange
        var mutation = @"
            mutation UpdateTenantPlan($input: UpdateTenantPlanInput!) {
                updateTenantPlan(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = Guid.NewGuid().ToString(),
                planType = "professional",
                retentionDays = 90
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<UpdateTenantPlanResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTenantPlan);
        Assert.False(response.Data.UpdateTenantPlan.Success);
        Assert.NotNull(response.Data.UpdateTenantPlan.Errors);
        Assert.NotEmpty(response.Data.UpdateTenantPlan.Errors);
        Assert.Contains(response.Data.UpdateTenantPlan.Errors, e => e.Code == "NOT_FOUND");
        Assert.Null(response.Data.UpdateTenantPlan.Tenant);
    }

    [Fact]
    public async Task UpdateTenantPlan_WithInvalidPlanType_ShouldReturnValidationError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant = new Tenant("Test Tenant", $"test-tenant-invalid-plan-{Guid.NewGuid():N}", "free", 30);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation UpdateTenantPlan($input: UpdateTenantPlanInput!) {
                updateTenantPlan(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    tenant {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                planType = "InvalidPlan",
                retentionDays = 90
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<UpdateTenantPlanResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTenantPlan);
        Assert.False(response.Data.UpdateTenantPlan.Success);
        Assert.NotNull(response.Data.UpdateTenantPlan.Errors);
        Assert.NotEmpty(response.Data.UpdateTenantPlan.Errors);
        Assert.Contains(response.Data.UpdateTenantPlan.Errors, e => e.Code == "VALIDATION_ERROR");
    }

    #endregion

    #region CreateWorkspace Tests

    [Fact]
    public async Task CreateWorkspace_WithValidInput_ShouldCreateWorkspace()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant = new Tenant("Test Tenant", $"test-tenant-create-ws-{Guid.NewGuid():N}", "free", 30);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateWorkspace($input: CreateWorkspaceInput!) {
                createWorkspace(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    workspace {
                        id
                        name
                        platform
                        externalId
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                name = "New Workspace",
                platform = "WhatsApp",
                externalId = $"ext-workspace-123-{Guid.NewGuid():N}"
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateWorkspaceResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Errors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateWorkspace);
        Assert.True(response.Data.CreateWorkspace.Success);
        Assert.Null(response.Data.CreateWorkspace.Errors);
        Assert.NotNull(response.Data.CreateWorkspace.Workspace);
        Assert.Equal("New Workspace", response.Data.CreateWorkspace.Workspace.Name);
        Assert.Equal("WhatsApp", response.Data.CreateWorkspace.Workspace.Platform);
        Assert.StartsWith("ext-workspace-123", response.Data.CreateWorkspace.Workspace.ExternalId);

        // Verify in database using the returned workspace ID
        var workspaceId = Guid.Parse(response.Data.CreateWorkspace.Workspace.Id.ToString());
        var workspace = await dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
        Assert.NotNull(workspace);
        Assert.Equal(tenant.Id, workspace!.TenantId);
    }

    [Fact]
    public async Task CreateWorkspace_WithNonExistentTenant_ShouldReturnError()
    {
        // Arrange
        var mutation = @"
            mutation CreateWorkspace($input: CreateWorkspaceInput!) {
                createWorkspace(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    workspace {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = Guid.NewGuid().ToString(),
                name = "Orphan Workspace",
                platform = "Telegram",
                externalId = $"ext-123-{Guid.NewGuid():N}"
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateWorkspaceResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateWorkspace);
        Assert.False(response.Data.CreateWorkspace.Success);
        Assert.NotNull(response.Data.CreateWorkspace.Errors);
        Assert.NotEmpty(response.Data.CreateWorkspace.Errors);
        Assert.Contains(response.Data.CreateWorkspace.Errors, e => e.Code == "NOT_FOUND");
        Assert.Null(response.Data.CreateWorkspace.Workspace);
    }

    [Fact]
    public async Task CreateWorkspace_WithDuplicateExternalId_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var uniqueExtId = $"duplicate-ext-id-{Guid.NewGuid():N}";
        var tenant = new Tenant("Test Tenant", $"test-tenant-dup-ws-{Guid.NewGuid():N}", "free", 30);
        var existingWorkspace = tenant.AddWorkspace("Existing Workspace", "WhatsApp");
        existingWorkspace.UpdateExternalId(uniqueExtId);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateWorkspace($input: CreateWorkspaceInput!) {
                createWorkspace(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    workspace {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                name = "Another Workspace",
                platform = "WhatsApp",
                externalId = uniqueExtId
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateWorkspaceResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);

        // Debug: Check for GraphQL errors
        if (response.Errors != null && response.Errors.Any())
        {
            var errorMessages = string.Join("\n", response.Errors.Select(e =>
            {
                var msg = e.Message;
                if (e.Extensions != null && e.Extensions.ContainsKey("message"))
                    msg += $" - {e.Extensions["message"]}";
                if (e.Extensions != null && e.Extensions.ContainsKey("stackTrace"))
                    msg += $"\nStackTrace: {e.Extensions["stackTrace"]}";
                return msg;
            }));
            throw new Exception($"GraphQL errors:\n{errorMessages}");
        }

        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateWorkspace);
        Assert.False(response.Data.CreateWorkspace.Success);
        Assert.NotNull(response.Data.CreateWorkspace.Errors);
        Assert.NotEmpty(response.Data.CreateWorkspace.Errors);
        Assert.Contains(response.Data.CreateWorkspace.Errors, e => e.Code == "CONFLICT");
    }

    #endregion

    #region CreateChannel Tests

    [Fact]
    public async Task CreateChannel_WithValidInput_ShouldCreateChannel()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant = new Tenant("Test Tenant", $"test-tenant-create-ch-{Guid.NewGuid():N}", "free", 30);
        var workspace = tenant.AddWorkspace("Test Workspace", "Telegram");
        workspace.UpdateExternalId($"ext-workspace-{Guid.NewGuid():N}");

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateChannel($input: CreateChannelInput!) {
                createChannel(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    channel {
                        id
                        name
                        externalId
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                workspaceId = workspace.Id.ToString(),
                name = "New Channel",
                externalId = $"ext-channel-123-{Guid.NewGuid():N}"
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateChannelResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Errors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateChannel);
        Assert.True(response.Data.CreateChannel.Success);
        Assert.Null(response.Data.CreateChannel.Errors);
        Assert.NotNull(response.Data.CreateChannel.Channel);
        Assert.Equal("New Channel", response.Data.CreateChannel.Channel.Name);
        Assert.StartsWith("ext-channel-123", response.Data.CreateChannel.Channel.ExternalId);

        // Verify in database using the returned channel ID
        var channelId = Guid.Parse(response.Data.CreateChannel.Channel.Id.ToString());
        var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == channelId);
        Assert.NotNull(channel);
        Assert.Equal(workspace.Id, channel!.WorkspaceId);
    }

    [Fact]
    public async Task CreateChannel_WithNonExistentWorkspace_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant = new Tenant("Test Tenant", $"test-tenant-orphan-ch-{Guid.NewGuid():N}", "free", 30);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateChannel($input: CreateChannelInput!) {
                createChannel(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    channel {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                workspaceId = Guid.NewGuid().ToString(),
                name = "Orphan Channel",
                externalId = $"ext-orphan-{Guid.NewGuid():N}"
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateChannelResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateChannel);
        Assert.False(response.Data.CreateChannel.Success);
        Assert.NotNull(response.Data.CreateChannel.Errors);
        Assert.NotEmpty(response.Data.CreateChannel.Errors);
        Assert.Contains(response.Data.CreateChannel.Errors, e => e.Code == "NOT_FOUND");
        Assert.Null(response.Data.CreateChannel.Channel);
    }

    [Fact]
    public async Task CreateChannel_WithDuplicateExternalId_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var uniqueChannelId = $"duplicate-channel-id-{Guid.NewGuid():N}";
        var tenant = new Tenant("Test Tenant", $"test-tenant-dup-ch-{Guid.NewGuid():N}", "free", 30);
        var workspace = tenant.AddWorkspace("Test Workspace", "WhatsApp");
        workspace.UpdateExternalId($"ext-workspace-{Guid.NewGuid():N}");

        var existingChannel = workspace.AddChannel("Existing Channel", uniqueChannelId);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        // Set TenantId shadow property on channel (not set automatically through aggregate)
        dbContext.Entry(existingChannel).Property("TenantId").CurrentValue = tenant.Id;
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateChannel($input: CreateChannelInput!) {
                createChannel(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    channel {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant.Id.ToString(),
                workspaceId = workspace.Id.ToString(),
                name = "Another Channel",
                externalId = uniqueChannelId
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateChannelResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);

        // Debug: Check for GraphQL errors
        if (response.Errors != null && response.Errors.Any())
        {
            var errorMessages = string.Join("\n", response.Errors.Select(e =>
            {
                var msg = e.Message;
                if (e.Extensions != null && e.Extensions.ContainsKey("message"))
                    msg += $" - {e.Extensions["message"]}";
                if (e.Extensions != null && e.Extensions.ContainsKey("stackTrace"))
                    msg += $"\nStackTrace: {e.Extensions["stackTrace"]}";
                return msg;
            }));
            throw new Exception($"GraphQL errors:\n{errorMessages}");
        }

        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateChannel);
        Assert.False(response.Data.CreateChannel.Success);
        Assert.NotNull(response.Data.CreateChannel.Errors);
        Assert.NotEmpty(response.Data.CreateChannel.Errors);
        Assert.Contains(response.Data.CreateChannel.Errors, e => e.Code == "CONFLICT");
    }

    [Fact]
    public async Task CreateChannel_WithMismatchedTenantAndWorkspace_ShouldReturnError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        var tenant1 = new Tenant("Tenant 1", $"tenant-1-mismatch-{Guid.NewGuid():N}", "free", 30);
        var tenant2 = new Tenant("Tenant 2", $"tenant-2-mismatch-{Guid.NewGuid():N}", "free", 30);
        var workspace = tenant2.AddWorkspace("Tenant2 Workspace", "Telegram");
        workspace.UpdateExternalId($"ext-workspace-t2-{Guid.NewGuid():N}");

        dbContext.Tenants.AddRange(tenant1, tenant2);
        await dbContext.SaveChangesAsync();

        var mutation = @"
            mutation CreateChannel($input: CreateChannelInput!) {
                createChannel(input: $input) {
                    success
                    errors {
                        message
                        code
                    }
                    channel {
                        id
                    }
                }
            }";

        var variables = new
        {
            input = new
            {
                tenantId = tenant1.Id.ToString(), // Using tenant1 but workspace belongs to tenant2
                workspaceId = workspace.Id.ToString(),
                name = "Mismatched Channel",
                externalId = $"ext-mismatch-{Guid.NewGuid():N}"
            }
        };

        // Act
        var response = await ExecuteGraphQLMutationAsync<CreateChannelResponse>(mutation, variables);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateChannel);
        Assert.False(response.Data.CreateChannel.Success);
        Assert.NotNull(response.Data.CreateChannel.Errors);
        Assert.NotEmpty(response.Data.CreateChannel.Errors);
        Assert.Contains(response.Data.CreateChannel.Errors, e => e.Code == "VALIDATION_ERROR" || e.Code == "NOT_FOUND");
    }

    #endregion

    #region Response DTOs

    private class CreateTenantResponse
    {
        public CreateTenantPayloadDto CreateTenant { get; set; } = new();
    }

    private class UpdateTenantPlanResponse
    {
        public UpdateTenantPlanPayloadDto UpdateTenantPlan { get; set; } = new();
    }

    private class CreateWorkspaceResponse
    {
        public CreateWorkspacePayloadDto CreateWorkspace { get; set; } = new();
    }

    private class CreateChannelResponse
    {
        public CreateChannelPayloadDto CreateChannel { get; set; } = new();
    }

    private class CreateTenantPayloadDto
    {
        public bool Success { get; set; }
        public List<UserErrorDto>? Errors { get; set; }
        public TenantDto? Tenant { get; set; }
    }

    private class UpdateTenantPlanPayloadDto
    {
        public bool Success { get; set; }
        public List<UserErrorDto>? Errors { get; set; }
        public TenantDto? Tenant { get; set; }
    }

    private class CreateWorkspacePayloadDto
    {
        public bool Success { get; set; }
        public List<UserErrorDto>? Errors { get; set; }
        public WorkspaceDto? Workspace { get; set; }
    }

    private class CreateChannelPayloadDto
    {
        public bool Success { get; set; }
        public List<UserErrorDto>? Errors { get; set; }
        public ChannelDto? Channel { get; set; }
    }

    private class UserErrorDto
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    private class TenantDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public int RetentionDays { get; set; }
    }

    private class WorkspaceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
    }

    private class ChannelDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
    }

    #endregion
}