using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.Description)
            .HasMaxLength(1024);

        builder.Property(s => s.ContextWindowTokens)
            .IsRequired()
            .HasDefaultValue(8192);

        builder.Property(s => s.MaxOutputTokens)
            .IsRequired()
            .HasDefaultValue(2048);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.LastActivityAt);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasOne(s => s.InferenceConfig)
            .WithMany()
            .HasForeignKey(s => s.InferenceConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.LastActivityAt);
        builder.HasIndex(s => s.CreatedAt);
    }
}
