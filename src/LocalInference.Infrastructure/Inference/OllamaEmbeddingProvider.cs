using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LocalInference.Application.Abstractions.Inference;
using Microsoft.Extensions.Logging;

namespace LocalInference.Infrastructure.Inference;

public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;
    private readonly string _embeddingModel;

    public string ProviderName => "Ollama";
    public int EmbeddingDimensions { get; } = 768;

    public OllamaEmbeddingProvider(HttpClient httpClient, ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _embeddingModel = "nomic-embed-text";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new OllamaEmbeddingRequest
        {
            Model = _embeddingModel,
            Prompt = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/embeddings",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
            cancellationToken: cancellationToken);

        if (result?.Embedding == null)
        {
            throw new InvalidOperationException("Failed to generate embedding");
        }

        return result.Embedding;
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }

        return embeddings;
    }

    private class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
