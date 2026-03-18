namespace LocalInference.Domain.ValueObjects;

public sealed record RetrievalResult
{
    public required string Content { get; init; }
    public required string Source { get; init; }
    public required double Score { get; init; }
    public required int TokenCount { get; init; }
    public string? DocumentType { get; init; }
    public string? Language { get; init; }
    public int? ChunkIndex { get; init; }

    public static RetrievalResult Create(
        string content,
        string source,
        double score,
        int tokenCount,
        string? documentType = null,
        string? language = null,
        int? chunkIndex = null)
    {
        return new RetrievalResult
        {
            Content = content,
            Source = source,
            Score = score,
            TokenCount = tokenCount,
            DocumentType = documentType,
            Language = language,
            ChunkIndex = chunkIndex
        };
    }
}
