using LocalInference.Domain.ValueObjects;

namespace LocalInference.Application.Abstractions.Retrieval;

public interface ITechnicalRetrievalService
{
    Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievalResult>> RetrieveForSessionAsync(
        Guid sessionId,
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken = default);

    Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task ReindexAllAsync(CancellationToken cancellationToken = default);
}

public sealed record RetrievalOptions
{
    public int MaxResults { get; init; } = 5;
    public int MaxTokens { get; init; } = 2000;
    public double MinScore { get; init; } = 0.7;
    public IReadOnlyList<string>? DocumentTypes { get; init; }
    public string? Language { get; init; }
    public bool PrioritizeRecent { get; init; } = true;
}
