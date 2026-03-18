namespace LocalInference.Application.Abstractions.Inference;

public interface IInferenceProvider
{
    string ProviderName { get; }

    Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<InferenceStreamChunk> StreamCompletionAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default);

    Task<int> EstimateTokenCountAsync(string text, string modelIdentifier, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

public sealed record InferenceRequest
{
    public required string ModelIdentifier { get; init; }
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public double Temperature { get; init; } = 0.7;
    public double TopP { get; init; } = 0.9;
    public int? MaxTokens { get; init; }
    public int? Seed { get; init; }
    public double? FrequencyPenalty { get; init; }
    public double? PresencePenalty { get; init; }
    public IReadOnlyList<string> StopSequences { get; init; } = Array.Empty<string>();
    public bool Stream { get; init; } = false;
}

public sealed record ChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public string? Name { get; init; }

    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
    public static ChatMessage User(string content, string? name = null) => new() { Role = "user", Content = content, Name = name };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}

public sealed record InferenceResponse
{
    public required string Id { get; init; }
    public required string Model { get; init; }
    public required string Content { get; init; }
    public required int PromptTokens { get; init; }
    public required int CompletionTokens { get; init; }
    public required int TotalTokens { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? FinishReason { get; init; }
}

public sealed record InferenceStreamChunk
{
    public required string Id { get; init; }
    public required string Model { get; init; }
    public string? DeltaContent { get; init; }
    public string? FinishReason { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }
}
