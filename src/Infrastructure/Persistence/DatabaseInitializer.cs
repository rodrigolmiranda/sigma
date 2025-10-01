using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sigma.Shared.Enums;

namespace Sigma.Infrastructure.Persistence;

public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SigmaDbContext>();

        try
        {
            _logger.LogInformation("Starting database migration...");

            // Apply any pending migrations
            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Database migration completed successfully");

            // Seed initial data if needed
            await SeedDataAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedDataAsync(SigmaDbContext context, CancellationToken cancellationToken)
    {
        // Check if we already have data
        if (await context.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding initial data...");

        // Add default tenant for development
        var defaultTenant = new Domain.Entities.Tenant(
            name: "Default Organization",
            slug: "default",
            planType: "free",
            retentionDays: 30
        );

        context.Tenants.Add(defaultTenant);

        // Add a sample workspace
        var workspace = defaultTenant.AddWorkspace("General", Platform.Slack);
        workspace.UpdateExternalId("W-DEFAULT");

        // Add a sample channel
        var channel = workspace.AddChannel("general", "C-GENERAL");

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Initial data seeded successfully");
    }
}