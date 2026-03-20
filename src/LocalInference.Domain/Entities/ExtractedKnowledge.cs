using LocalInference.Domain.Common;

namespace LocalInference.Domain.Entities;

/// <summary>
/// Represents extracted knowledge from conversations (long-term memory).
/// </summary>
public class ExtractedKnowledge : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
    public string Embedding { get; set; } = string.Empty; // JSON serialized embedding vector
    public Guid SessionId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public double ConfidenceScore { get; set; } = 1.0; // How confident we are in this extraction
    public string SourceType { get; set; } = "conversation"; // conversation, document, etc.

    // Not mapped property for the actual embedding vector
    [NotMapped]
    public float[] EmbeddingVector
    {
        get => string.IsNullOrEmpty(Embedding) ? Array.Empty<float>() : System.Text.Json.JsonSerializer.Deserialize<float[]>(Embedding);
        set => Embedding = value == null || value.Length == 0 ? string.Empty : System.Text.Json.JsonSerializer.Serialize(value);
    }
}