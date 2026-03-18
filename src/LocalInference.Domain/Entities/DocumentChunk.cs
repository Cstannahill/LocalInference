using LocalInference.Domain.Common;

namespace LocalInference.Domain.Entities;

public sealed class DocumentChunk : AuditableEntity
{
    public Guid TechnicalDocumentId { get; private set; }
    public TechnicalDocument TechnicalDocument { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public int StartPosition { get; private set; }
    public int EndPosition { get; private set; }
    public int TokenCount { get; private set; }
    public int ChunkIndex { get; private set; }
    public string? EmbeddingJson { get; private set; }
    public float[]? Embedding
    {
        get
        {
            if (string.IsNullOrEmpty(EmbeddingJson))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<float[]>(EmbeddingJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            EmbeddingJson = value != null ? System.Text.Json.JsonSerializer.Serialize(value) : null;
        }
    }

    private DocumentChunk() { }

    public static DocumentChunk Create(
        Guid technicalDocumentId,
        string content,
        int startPosition,
        int endPosition,
        int chunkIndex)
    {
        return new DocumentChunk
        {
            TechnicalDocumentId = technicalDocumentId,
            Content = content,
            StartPosition = startPosition,
            EndPosition = endPosition,
            TokenCount = EstimateTokenCount(content),
            ChunkIndex = chunkIndex
        };
    }

    public void SetEmbedding(float[] embedding)
    {
        Embedding = embedding;
        MarkUpdated();
    }

    private static int EstimateTokenCount(string text)
    {
        return (int)(text.Length / 4.0);
    }
}
