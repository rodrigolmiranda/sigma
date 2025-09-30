using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigma.Domain.Entities;

namespace Sigma.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Platform)
            .HasColumnName("platform")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(200);

        builder.HasIndex(w => new { w.TenantId, w.Platform, w.ExternalId })
            .HasDatabaseName("ix_workspaces_tenant_platform_external")
            .IsUnique()
            .HasFilter("external_id IS NOT NULL");

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.LastSyncAtUtc)
            .HasColumnName("last_sync_at_utc");

        builder.Property(w => w.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(w => w.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);

        builder.HasMany(w => w.Channels)
            .WithOne()
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add shadow property for tenant isolation
        builder.Property<Guid>("TenantId")
            .HasColumnName("tenant_id");

        builder.Ignore(w => w.DomainEvents);
    }
}