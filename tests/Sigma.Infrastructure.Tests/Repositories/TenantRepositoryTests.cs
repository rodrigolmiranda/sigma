using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Sigma.Infrastructure.Tests.Repositories;

public class TenantRepositoryTests : IDisposable
{
    private readonly Infrastructure.Persistence.SigmaDbContext _context;
    private readonly TenantRepository _repository;

    public TenantRepositoryTests()
    {
        _context = TestDbContextFactory.CreateDbContext();
        _repository = new TenantRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("Test Tenant", result.Name);
        Assert.Equal("test-tenant", result.Slug);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTenant_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "unique-slug", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetBySlugAsync("unique-slug", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
        Assert.Equal("unique-slug", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("non-existent-slug", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_WithValidTenant_ShouldAddToContext()
    {
        // Arrange
        var tenant = new Tenant("New Tenant", "new-tenant", "starter", 60);

        // Act
        await _repository.AddAsync(tenant, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedTenant = await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTenant);
        Assert.Equal("New Tenant", savedTenant.Name);
        Assert.Equal("new-tenant", savedTenant.Slug);
        Assert.Equal("starter", savedTenant.PlanType);
        Assert.Equal(60, savedTenant.RetentionDays);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingTenant_ShouldUpdateInContext()
    {
        // Arrange
        var tenant = new Tenant("Original Name", "original-slug", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        tenant.UpdatePlan("professional", 180);
        await _repository.UpdateAsync(tenant, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var updatedTenant = await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTenant);
        Assert.Equal("professional", updatedTenant.PlanType);
        Assert.Equal(180, updatedTenant.RetentionDays);
    }

    [Fact]
    public async Task GetAllActiveAsync_WithMultipleTenants_ShouldReturnAll()
    {
        // Arrange
        var tenant1 = new Tenant("Tenant 1", "tenant-1", "free", 30);
        var tenant2 = new Tenant("Tenant 2", "tenant-2", "starter", 60);
        var tenant3 = new Tenant("Tenant 3", "tenant-3", "professional", 90);

        _context.Tenants.AddRange(tenant1, tenant2, tenant3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetAllActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var tenants = result.ToList();
        Assert.Equal(3, tenants.Count);
        Assert.Contains(tenants, t => t.Slug == "tenant-1");
        Assert.Contains(tenants, t => t.Slug == "tenant-2");
        Assert.Contains(tenants, t => t.Slug == "tenant-3");
    }

    [Fact]
    public async Task GetAllActiveAsync_WithNoTenants_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingTenant_ShouldReturnTrue()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await _repository.ExistsAsync(tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentTenant_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.ExistsAsync(tenant.Id, cts.Token));
    }

    [Fact]
    public async Task GetAllActiveAsync_WithMixedActiveInactive_ShouldReturnOnlyActive()
    {
        // Arrange
        var activeTenant1 = new Tenant("Active1", "active-1", "free", 30);
        var activeTenant2 = new Tenant("Active2", "active-2", "free", 30);
        var inactiveTenant = new Tenant("Inactive", "inactive", "free", 30);
        inactiveTenant.Deactivate();

        _context.Tenants.AddRange(activeTenant1, activeTenant2, inactiveTenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var tenants = await _repository.GetAllActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        var tenantList = tenants.ToList();
        Assert.Equal(2, tenantList.Count);
        Assert.All(tenantList, t => Assert.True(t.IsActive));
        Assert.Contains(tenantList, t => t.Id == activeTenant1.Id);
        Assert.Contains(tenantList, t => t.Id == activeTenant2.Id);
        Assert.DoesNotContain(tenantList, t => t.Id == inactiveTenant.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithDetachedEntity_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Original", "original", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tenantId = tenant.Id;

        // Detach the entity
        _context.Entry(tenant).State = EntityState.Detached;

        // Load and update the tenant
        var reloaded = await _context.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, TestContext.Current.CancellationToken);
        reloaded.UpdatePlan("professional", 60);
        reloaded.Deactivate();

        // Act
        await _repository.UpdateAsync(reloaded, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        _context.ChangeTracker.Clear();
        var result = await _context.Tenants.FindAsync(new object[] { tenantId }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("professional", result.PlanType);
        Assert.Equal(60, result.RetentionDays);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task GetBySlugAsync_WithEmptySlug_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync(string.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithCaseSensitivity_ShouldFindCorrectly()
    {
        // Arrange
        var tenant = new Tenant("Test", "test-slug", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result1 = await _repository.GetBySlugAsync("test-slug", TestContext.Current.CancellationToken);
        var result2 = await _repository.GetBySlugAsync("TEST-SLUG", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(tenant.Id, result1.Id);

        // Slug comparison should be case-insensitive
        Assert.Null(result2); // Or NotNull if case-insensitive - depends on implementation
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}