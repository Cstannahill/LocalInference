namespace LocalInference.Application.Abstractions.Summarization;

public interface ITechnicalSummarizationService
{
    Task<string> SummarizeConversationAsync(
        IReadOnlyList<MessageSummary> messages,
        SummarizationOptions options,
        CancellationToken cancellationToken = default);

    Task<string> CompressTechnicalContextAsync(
        string context,
        CompressionOptions options,
        CancellationToken cancellationToken = default);

    Task<TechnicalSummary> SummarizeForRetrievalAsync(
        string content,
        string documentType,
        CancellationToken cancellationToken = default);
}

public sealed record MessageSummary
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; }
}

public sealed record SummarizationOptions
{
    public int TargetTokenCount { get; init; } = 500;
    public bool PreserveKeyFacts { get; init; } = true;
    public bool PreserveTechnicalDetails { get; init; } = true;
    public string? FocusArea { get; init; }
}

public sealed record CompressionOptions
{
    public int TargetTokenCount { get; init; } = 300;
    public bool MaintainStructure { get; init; } = true;
    public bool PreserveCodeBlocks { get; init; } = true;
}

public sealed record TechnicalSummary
{
    public required string Summary { get; init; }
    public required string KeyPoints { get; init; }
    public required string TechnicalTerms { get; init; }
    public int OriginalTokenCount { get; init; }
    public int CompressedTokenCount { get; init; }
    public double CompressionRatio { get; init; }
}
