using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigma.Domain.Entities;
using Sigma.Domain.ValueObjects;
using System.Text.Json;

namespace Sigma.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.ChannelId)
            .HasColumnName("channel_id")
            .IsRequired();

        builder.Property(m => m.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(m => m.PlatformMessageId)
            .HasColumnName("platform_message_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(m => new { m.ChannelId, m.PlatformMessageId })
            .HasDatabaseName("ix_messages_channel_platform")
            .IsUnique();

        builder.HasIndex(m => new { m.TenantId, m.TimestampUtc })
            .HasDatabaseName("ix_messages_tenant_timestamp");

        // Configure Sender value object
        builder.OwnsOne(m => m.Sender, sender =>
        {
            sender.Property(s => s.PlatformUserId)
                .HasColumnName("sender_platform_user_id")
                .HasMaxLength(200)
                .IsRequired();

            sender.Property(s => s.DisplayName)
                .HasColumnName("sender_display_name")
                .HasMaxLength(200);

            sender.Property(s => s.IsBot)
                .HasColumnName("sender_is_bot")
                .IsRequired();
        });

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Text)
            .HasColumnName("text");

        builder.Property(m => m.TimestampUtc)
            .HasColumnName("timestamp_utc")
            .IsRequired();

        builder.Property(m => m.EditedAtUtc)
            .HasColumnName("edited_at_utc");

        builder.Property(m => m.ReplyToPlatformMessageId)
            .HasColumnName("reply_to_platform_message_id")
            .HasMaxLength(200);

        builder.Property(m => m.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValue(false);

        // Configure Reactions collection as JSON
        builder.Property(m => m.Reactions)
            .HasColumnName("reactions")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<MessageReaction>>(v, (JsonSerializerOptions?)null) ?? new List<MessageReaction>())
            .HasColumnType("jsonb");

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired(false);

        builder.Ignore(m => m.DomainEvents);
    }
}