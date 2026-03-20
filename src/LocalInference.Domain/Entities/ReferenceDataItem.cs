using LocalInference.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LocalInference.Domain.Entities;

public class ReferenceDataItem : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
    public string Embedding { get; set; } = string.Empty; // JSON serialized embedding vector
    public Guid ReferenceDataId { get; set; }

    // Not mapped property for the actual embedding vector
    [NotMapped]
    public float[] EmbeddingVector
    {
        get => string.IsNullOrEmpty(Embedding) ? Array.Empty<float>() : JsonSerializer.Deserialize<float[]>(Embedding);
        set => Embedding = value == null || value.Length == 0 ? string.Empty : JsonSerializer.Serialize(value);
    }
}