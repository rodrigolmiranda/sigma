using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigma.Domain.Entities;

namespace Sigma.Infrastructure.Persistence.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Platform)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ProcessingError)
            .HasMaxLength(4000)
            .IsRequired(false);

        // Unique index to ensure idempotency
        builder.HasIndex(x => new { x.Platform, x.EventId })
            .IsUnique()
            .HasDatabaseName("IX_WebhookEvents_Platform_EventId");

        // Index for querying unprocessed events
        builder.HasIndex(x => x.ProcessedAtUtc)
            .HasFilter("\"ProcessedAtUtc\" IS NULL")
            .HasDatabaseName("IX_WebhookEvents_Unprocessed");

        // Explicitly configure UpdatedAtUtc as nullable
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);
    }
}