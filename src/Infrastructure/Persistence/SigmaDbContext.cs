using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Common;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence.Configurations;
using System.Linq.Expressions;
using System.Text.Json;

namespace Sigma.Infrastructure.Persistence;

public class SigmaDbContext : DbContext
{
    public ITenantContext? TenantContext { get; set; }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    public SigmaDbContext(DbContextOptions<SigmaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
        modelBuilder.ApplyConfiguration(new ChannelConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookEventConfiguration());

        // Apply tenant isolation global filters
        if (TenantContext != null && TenantContext.TenantId != Guid.Empty)
        {
            // Workspace filter
            modelBuilder.Entity<Workspace>().HasQueryFilter(w => w.TenantId == TenantContext.TenantId);

            // Channel filter (through workspace)
            modelBuilder.Entity<Channel>().HasQueryFilter(c =>
                EF.Property<Guid>(c, "TenantId") == TenantContext.TenantId);

            // Message filter
            modelBuilder.Entity<Message>().HasQueryFilter(m => m.TenantId == TenantContext.TenantId);
        }

        // Configure value conversions and precision
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // All DateTime properties stored as UTC (PostgreSQL handles this natively)
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.CurrentValues["CreatedAtUtc"] = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.CurrentValues["UpdatedAtUtc"] = DateTime.UtcNow;
            }
        }

        // Capture domain events before saving
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Persist domain events to outbox before saving the main transaction
        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage(
                domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }));

            await OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after successfully saving (including outbox)
        foreach (var entity in ChangeTracker.Entries<Entity>())
        {
            entity.Entity.ClearDomainEvents();
        }

        return result;
    }

    public virtual async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}