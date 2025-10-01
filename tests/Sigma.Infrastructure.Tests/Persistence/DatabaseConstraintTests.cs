using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;
using Sigma.Infrastructure.Tests.TestHelpers;
using Xunit;

namespace Sigma.Infrastructure.Tests.Persistence;

/// <summary>
/// Tests that require real database constraints (unique indexes, foreign keys, etc.)
/// These tests use PostgreSQL instead of in-memory database.
/// </summary>
public class DatabaseConstraintTests : PostgresTestBase
{
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Guid _tenantId;

    public DatabaseConstraintTests()
    {
        _tenantId = Guid.NewGuid();
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _tenantContextMock.Setup(x => x.TenantSlug).Returns($"test-tenant-{_tenantId:N}");
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.TenantContext = _tenantContextMock.Object;
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateSlug_ShouldThrowException()
    {
        // Arrange
        var duplicateSlug = $"duplicate-slug-{Guid.NewGuid():N}";
        var tenant1 = new Tenant("Tenant1", duplicateSlug, "free", 30);
        var tenant2 = new Tenant("Tenant2", duplicateSlug, "free", 30); // Duplicate slug

        Context.Tenants.Add(tenant1);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Tenants.Add(tenant2);

        // Act & Assert
        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
            await Context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Verify first tenant still exists
        var existingTenant = await Context.Tenants.FindAsync(new object[] { tenant1.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(existingTenant);
    }

    [Fact]
    public async Task ModelConfiguration_ShouldEnforceUniqueTenantSlug()
    {
        // Arrange
        var duplicateSlug = $"test-slug-{Guid.NewGuid():N}";
        var tenant1 = new Tenant("Test1", duplicateSlug, "free", 30);
        var tenant2 = new Tenant("Test2", duplicateSlug, "free", 30); // Same slug

        Context.Tenants.Add(tenant1);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Tenants.Add(tenant2);

        // Act & Assert - Should throw due to unique constraint on Slug
        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
            await Context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnitOfWork_SaveEntitiesAsync_WithDuplicateSlug_ShouldReturnFalse()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(Context);
        var duplicateSlug = $"dup-slug-{Guid.NewGuid():N}";
        var tenant1 = new Tenant("Test1", duplicateSlug, "free", 30);
        var tenant2 = new Tenant("Test2", duplicateSlug, "free", 30);

        Context.Tenants.Add(tenant1);
        var result1 = await unitOfWork.SaveEntitiesAsync();
        Assert.True(result1);

        Context.Tenants.Add(tenant2);

        // Act
        var result2 = await unitOfWork.SaveEntitiesAsync();

        // Assert
        Assert.False(result2);
    }
}
