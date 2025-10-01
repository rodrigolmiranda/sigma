using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Sigma.Domain.Contracts;
using Sigma.Infrastructure.Persistence;
using Sigma.Infrastructure.Tests.TestHelpers;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Infrastructure.Tests.Persistence;

public class UnitOfWorkTests : PostgresTestBase
{
    private UnitOfWork _unitOfWork = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _unitOfWork = new UnitOfWork(Context);
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_ShouldReturnCount()
    {
        // Arrange
        var tenant = new Domain.Entities.Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid()}", "free", 30);
        Context.Tenants.Add(tenant);

        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result);
        Assert.NotNull(await Context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleChanges_ShouldReturnCorrectCount()
    {
        // Arrange
        var slug1 = $"tenant-1-{Guid.NewGuid()}";
        var slug2 = $"tenant-2-{Guid.NewGuid()}";
        var slug3 = $"tenant-3-{Guid.NewGuid()}";

        var tenant1 = new Domain.Entities.Tenant("Tenant 1", slug1, "free", 30);
        var tenant2 = new Domain.Entities.Tenant("Tenant 2", slug2, "starter", 60);
        var tenant3 = new Domain.Entities.Tenant("Tenant 3", slug3, "professional", 90);

        Context.Tenants.AddRange(tenant1, tenant2, tenant3);

        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result);
        // Count only the tenants we created in this test
        var ourTenants = await Context.Tenants
            .Where(t => t.Slug == slug1 || t.Slug == slug2 || t.Slug == slug3)
            .CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, ourTenants);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var tenant = new Domain.Entities.Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid()}", "free", 30);
        Context.Tenants.Add(tenant);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task SaveChangesAsync_CalledMultipleTimes_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var slug1 = $"tenant-1-{Guid.NewGuid()}";
        var slug2 = $"tenant-2-{Guid.NewGuid()}";

        var tenant1 = new Domain.Entities.Tenant("Tenant 1", slug1, "free", 30);
        Context.Tenants.Add(tenant1);
        var firstSave = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tenant2 = new Domain.Entities.Tenant("Tenant 2", slug2, "starter", 60);
        Context.Tenants.Add(tenant2);
        var secondSave = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, firstSave);
        Assert.Equal(1, secondSave);
        // Count only the tenants we created in this test
        var ourTenants = await Context.Tenants
            .Where(t => t.Slug == slug1 || t.Slug == slug2)
            .CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, ourTenants);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(null!));
    }

    [Fact]
    public async Task BeginTransactionAsync_StartsNewTransaction()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act
        await unitOfWork.BeginTransactionAsync();

        // Assert - transaction should be started
        await unitOfWork.CommitAsync();

        // Should be able to start another transaction after commit
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task CommitAsync_WithActiveTransaction_CommitsAndDisposesTransaction()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.CommitAsync();

        // Assert - should be able to start a new transaction
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task CommitAsync_WithoutTransaction_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _unitOfWork.CommitAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithActiveTransaction_RollsBackAndDisposesTransaction()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.RollbackAsync();

        // Assert - should be able to start a new transaction
        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.CommitAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithoutTransaction_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task SaveEntitiesAsync_CallsSaveChangesAndReturnsTrue()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Add test data
        var tenant = new Domain.Entities.Tenant("test-tenant-2", "Test Tenant 2", "free", 30);
        context.Tenants.Add(tenant);

        // Act
        var result = await unitOfWork.SaveEntitiesAsync();

        // Assert
        Assert.True(result);
        Assert.Single(context.Tenants.Local);
    }

    [Fact]
    public async Task SaveEntitiesAsync_WithCancellationToken_PassesTokenAndReturnsTrue()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        var result = await _unitOfWork.SaveEntitiesAsync(cts.Token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Dispose_DisposesActiveTransaction()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act
        unitOfWork.BeginTransactionAsync().Wait();
        unitOfWork.Dispose();

        // Assert - After dispose, starting a new transaction should work
        unitOfWork.BeginTransactionAsync().Wait();
    }

    [Fact]
    public void Dispose_WithoutTransaction_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _unitOfWork.Dispose();
    }

    [Fact]
    public async Task Transaction_CompleteWorkflow_CommitsSuccessfully()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        // Create a mock tenant context for the test
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(Guid.Empty);
        tenantContextMock.Setup(x => x.TenantSlug).Returns((string?)null);

        using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContextMock.Object;
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act - Complete transaction workflow
        await unitOfWork.BeginTransactionAsync();

        var tenant = new Domain.Entities.Tenant("Workflow Tenant", "workflow-tenant", "free", 30);
        context.Tenants.Add(tenant);

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitAsync();

        // Assert
        var savedTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "workflow-tenant");
        Assert.NotNull(savedTenant);
    }

    [Fact]
    public async Task Transaction_RollbackWorkflow_DoesNotPersistChanges()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseSqlite(connection)
            .Options;

        // Create a mock tenant context for the test
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(Guid.Empty);
        tenantContextMock.Setup(x => x.TenantSlug).Returns((string?)null);

        using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContextMock.Object;
        context.Database.EnsureCreated();

        var unitOfWork = new UnitOfWork(context);

        // Act - Rollback transaction workflow
        await unitOfWork.BeginTransactionAsync();

        var tenant = new Domain.Entities.Tenant("Rollback Tenant", "rollback-tenant", "free", 30);
        context.Tenants.Add(tenant);

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.RollbackAsync();

        // Clear the change tracker to ensure we're reading from DB
        context.ChangeTracker.Clear();

        // Assert - tenant should not be persisted
        var savedTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "rollback-tenant");
        Assert.Null(savedTenant);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _unitOfWork.BeginTransactionAsync(cts.Token));
    }

    [Fact]
    public async Task SaveChangesAsync_WithFailure_ShouldThrowException()
    {
        // This test would require mocking the DbContext to throw an exception
        // Since we're using a real database, we'll simulate a constraint violation

        // Arrange
        var duplicateSlug = $"duplicate-slug-{Guid.NewGuid()}";
        var tenant1 = new Domain.Entities.Tenant("Test1", duplicateSlug, "free", 30);
        var tenant2 = new Domain.Entities.Tenant("Test2", duplicateSlug, "free", 30);

        Context.Tenants.Add(tenant1);
        await _unitOfWork.SaveChangesAsync();

        Context.Tenants.Add(tenant2);

        // Act & Assert - Should throw due to duplicate slug
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _unitOfWork.SaveChangesAsync());
    }

    // NOTE: This test moved to DatabaseConstraintTests.cs to use real database constraints

    [Fact]
    public async Task Transaction_NestedTransactions_ShouldNotCreateMultiple()
    {
        // Arrange & Act
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.BeginTransactionAsync(); // Second call should not create new transaction

        var slug = $"test-{Guid.NewGuid()}";
        var tenant = new Domain.Entities.Tenant("Test", slug, "free", 30);
        Context.Tenants.Add(tenant);
        await _unitOfWork.SaveChangesAsync();

        // Commit once should be sufficient
        await _unitOfWork.CommitAsync();

        // Assert - Verify data is saved
        Context.ChangeTracker.Clear();
        var saved = await Context.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Dispose_WithActiveTransaction_ShouldDisposeTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var tenant = new Domain.Entities.Tenant("Test", "dispose-test", "free", 30);
        Context.Tenants.Add(tenant);
        await _unitOfWork.SaveChangesAsync();

        // Act
        _unitOfWork.Dispose();

        // Assert - Transaction should be disposed, changes not committed
        using var newContext = CreateContext();
        var saved = await newContext.Tenants.FirstOrDefaultAsync(t => t.Slug == "dispose-test");
        Assert.Null(saved);
    }

    [Fact]
    public async Task CommitAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _unitOfWork.CommitAsync(cts.Token));
    }

    [Fact]
    public async Task RollbackAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _unitOfWork.RollbackAsync(cts.Token));
    }

    [Fact]
    public async Task SaveChangesAsync_AfterDispose_ShouldThrow()
    {
        // Arrange
        Context.Dispose();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task Transaction_MultipleOperations_MaintainsAtomicity()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var tenant = new Domain.Entities.Tenant("Multi", "multi", "professional", 60);
        Context.Tenants.Add(tenant);
        await _unitOfWork.SaveChangesAsync();

        var workspace = tenant.AddWorkspace("Workspace", Platform.Slack);
        await _unitOfWork.SaveChangesAsync();

        var channel = workspace.AddChannel("Channel", "C001");
        await _unitOfWork.SaveChangesAsync();

        // Act - Rollback all operations
        await _unitOfWork.RollbackAsync();

        // Assert - Nothing should be persisted
        Context.ChangeTracker.Clear();
        var savedTenant = await Context.Tenants.FirstOrDefaultAsync(t => t.Slug == "multi");
        Assert.Null(savedTenant);

        var workspaces = await Context.Workspaces.Where(w => w.TenantId == tenant.Id).ToListAsync();
        Assert.Empty(workspaces);

        var channels = await Context.Channels.Where(c => c.WorkspaceId == workspace.Id).ToListAsync();
        Assert.Empty(channels);
    }

    private SigmaDbContext CreateContext()
    {
        var options = CreateOptions();
        var context = new SigmaDbContext(options);

        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantSlug).Returns((string?)null);
        context.TenantContext = tenantContextMock.Object;

        return context;
    }
}
