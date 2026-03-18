using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class ContextMessageConfiguration : IEntityTypeConfiguration<ContextMessage>
{
    public void Configure(EntityTypeBuilder<ContextMessage> builder)
    {
        builder.ToTable("ContextMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SessionId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.TokenCount)
            .IsRequired();

        builder.Property(m => m.SequenceNumber)
            .IsRequired();

        builder.Property(m => m.IsSummarized)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.CheckpointId);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.SessionId);
        builder.HasIndex(m => new { m.SessionId, m.SequenceNumber });
        builder.HasIndex(m => m.IsSummarized);
    }
}
