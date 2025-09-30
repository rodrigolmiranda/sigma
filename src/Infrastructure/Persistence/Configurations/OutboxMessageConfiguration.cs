using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigma.Domain.Entities;

namespace Sigma.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventData)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.Error)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.NextRetryAtUtc)
            .IsRequired(false);

        // Indexes for efficient queries
        builder.HasIndex(x => x.ProcessedAtUtc)
            .HasFilter("\"ProcessedAtUtc\" IS NULL");

        builder.HasIndex(x => x.NextRetryAtUtc)
            .HasFilter("\"ProcessedAtUtc\" IS NULL");

        builder.HasIndex(x => x.CreatedAtUtc);

        // Explicitly configure UpdatedAtUtc as nullable
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);
    }
}