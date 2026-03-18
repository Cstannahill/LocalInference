using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalInference.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TechnicalDocumentId)
            .IsRequired();

        builder.Property(c => c.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(c => c.StartPosition)
            .IsRequired();

        builder.Property(c => c.EndPosition)
            .IsRequired();

        builder.Property(c => c.TokenCount)
            .IsRequired();

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        builder.Property(c => c.EmbeddingJson)
            .HasColumnType("jsonb");

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.TechnicalDocument)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.TechnicalDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TechnicalDocumentId);
        builder.HasIndex(c => c.ChunkIndex);
    }
}
