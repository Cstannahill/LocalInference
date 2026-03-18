using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class ContextCheckpointConfiguration : IEntityTypeConfiguration<ContextCheckpoint>
{
    public void Configure(EntityTypeBuilder<ContextCheckpoint> builder)
    {
        builder.ToTable("ContextCheckpoints");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SessionId)
            .IsRequired();

        builder.Property(c => c.StartMessageIndex)
            .IsRequired();

        builder.Property(c => c.EndMessageIndex)
            .IsRequired();

        builder.Property(c => c.Summary)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(c => c.OriginalTokenCount)
            .IsRequired();

        builder.Property(c => c.CompressedTokenCount)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.ReplacedByCheckpointId);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.Session)
            .WithMany(s => s.Checkpoints)
            .HasForeignKey(c => c.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.SessionId);
        builder.HasIndex(c => c.IsActive);
    }
}
