namespace LocalInference.Application.Abstractions.Inference;

public interface IEmbeddingProvider
{
    string ProviderName { get; }
    int EmbeddingDimensions { get; }

    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
