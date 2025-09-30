using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Infrastructure.Persistence;

namespace Sigma.Infrastructure.Tests.TestHelpers;

public static class InMemoryDbContextFactory
{
    public static SigmaDbContext Create(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();

        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .UseInternalServiceProvider(serviceProvider)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                // Ignore the warning about different service providers
                warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
            })
            .Options;

        var context = new SigmaDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static DbContextOptions<SigmaDbContext> CreateOptions(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();

        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        return new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .UseInternalServiceProvider(serviceProvider)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
            })
            .Options;
    }
}