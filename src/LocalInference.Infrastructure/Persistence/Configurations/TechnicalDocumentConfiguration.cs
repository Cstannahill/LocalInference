using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class TechnicalDocumentConfiguration : IEntityTypeConfiguration<TechnicalDocument>
{
    public void Configure(EntityTypeBuilder<TechnicalDocument> builder)
    {
        builder.ToTable("TechnicalDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(d => d.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(d => d.SourceUrl)
            .HasMaxLength(2048);

        builder.Property(d => d.SourcePath)
            .HasMaxLength(2048);

        builder.Property(d => d.Language)
            .HasMaxLength(64);

        builder.Property(d => d.Framework)
            .HasMaxLength(128);

        builder.Property(d => d.Version)
            .HasMaxLength(64);

        builder.Property(d => d.TokenCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.IsIndexed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.LastIndexedAt);

        builder.Property(d => d.EmbeddingModel)
            .HasMaxLength(256);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .IsRequired();

        builder.HasIndex(d => d.DocumentType);
        builder.HasIndex(d => d.IsIndexed);
        builder.HasIndex(d => d.Language);
        builder.HasIndex(d => d.Framework);
    }
}
