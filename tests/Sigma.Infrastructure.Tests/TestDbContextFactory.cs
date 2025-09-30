using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sigma.Domain.Contracts;
using Sigma.Infrastructure.Persistence;

namespace Sigma.Infrastructure.Tests;

public static class TestDbContextFactory
{
    private static readonly object _lock = new();
    private static int _contextCounter = 0;

    public static SigmaDbContext CreateDbContext(Guid? tenantId = null, bool bypassTenantFilters = false)
    {
        // Generate a unique database name to ensure complete isolation
        string databaseName;
        lock (_lock)
        {
            _contextCounter++;
            databaseName = $"TestDb_{Guid.NewGuid():N}_{_contextCounter}";
        }

        // Create a new service provider for each context to avoid shared caches
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .UseInternalServiceProvider(serviceProvider)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            // Use EnableSensitiveDataLogging only in tests for better error messages
            .EnableSensitiveDataLogging()
            .Options;

        ITenantContext tenantContext;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            var mock = new Mock<ITenantContext>();
            mock.Setup(x => x.TenantId).Returns(tenantId.Value);
            mock.Setup(x => x.TenantSlug).Returns($"test-tenant-{tenantId.Value:N}");
            tenantContext = mock.Object;
        }
        else
        {
            // Create a mock for system/anonymous context
            var mock = new Mock<ITenantContext>();
            mock.Setup(x => x.TenantId).Returns(Guid.Empty);
            mock.Setup(x => x.TenantSlug).Returns((string?)null);
            tenantContext = mock.Object;
        }

        var context = new SigmaDbContext(options);
        context.TenantContext = tenantContext;
        context.Database.EnsureCreated();

        return context;
    }
}