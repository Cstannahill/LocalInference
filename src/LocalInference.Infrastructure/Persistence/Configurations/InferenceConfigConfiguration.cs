using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class InferenceConfigConfiguration : IEntityTypeConfiguration<InferenceConfig>
{
    public void Configure(EntityTypeBuilder<InferenceConfig> builder)
    {
        builder.ToTable("InferenceConfigs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ModelIdentifier)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ProviderType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Temperature)
            .IsRequired()
            .HasDefaultValue(0.7);

        builder.Property(c => c.TopP)
            .IsRequired()
            .HasDefaultValue(0.9);

        builder.Property(c => c.TopK);

        builder.Property(c => c.RepeatPenalty);

        builder.Property(c => c.MaxTokens);

        builder.Property(c => c.ContextWindow);

        builder.Property(c => c.StopSequences)
            .HasMaxLength(1024);

        builder.Property(c => c.SystemPrompt)
            .HasColumnType("text");

        builder.Property(c => c.Seed);

        builder.Property(c => c.FrequencyPenalty);

        builder.Property(c => c.PresencePenalty);

        builder.Property(c => c.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasIndex(c => c.IsDefault)
            .HasFilter("\"IsDefault\" = true");

        builder.HasIndex(c => c.ProviderType);
    }
}
