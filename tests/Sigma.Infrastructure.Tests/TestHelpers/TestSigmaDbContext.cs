using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Contracts;
using Sigma.Infrastructure.Persistence;

namespace Sigma.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Test-specific DbContext that properly handles tenant isolation in tests.
/// This class ensures that each test gets a completely isolated context
/// without model caching issues that can occur with query filters.
/// </summary>
public class TestSigmaDbContext : SigmaDbContext
{
    private readonly Guid? _overrideTenantId;
    private readonly bool _bypassTenantFilters;

    public bool ShouldThrowOnConnect { get; set; }

    public TestSigmaDbContext(
        DbContextOptions<SigmaDbContext> options,
        ITenantContext? tenantContext = null,
        Guid? overrideTenantId = null,
        bool bypassTenantFilters = false)
        : base(options)
    {
        TenantContext = tenantContext ?? CreateDefaultTenantContext();
        _overrideTenantId = overrideTenantId;
        _bypassTenantFilters = bypassTenantFilters;
    }

    public TestSigmaDbContext(DbContextOptions<SigmaDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base configuration
        base.OnModelCreating(modelBuilder);

        // In test mode, we want to be able to bypass tenant filters when needed
        // This helps with test isolation and debugging
        if (_bypassTenantFilters)
        {
            // Remove all query filters for testing
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetQueryFilter(null);
            }
        }
    }

    public override async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowOnConnect)
        {
            return false;
        }
        return await base.CanConnectAsync(cancellationToken);
    }

    private static ITenantContext CreateDefaultTenantContext()
    {
        return new TestTenantContext(Guid.Empty, null);
    }

    private class TestTenantContext : ITenantContext
    {
        public TestTenantContext(Guid tenantId, string? tenantSlug)
        {
            TenantId = tenantId;
            TenantSlug = tenantSlug;
        }

        public Guid TenantId { get; }
        public string? TenantSlug { get; }
    }
}