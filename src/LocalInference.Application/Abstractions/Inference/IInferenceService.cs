using LocalInference.Domain.Entities;

namespace LocalInference.Application.Abstractions.Inference;

/// <summary>
/// Service for generating text completions using various inference providers.
/// </summary>
public interface IInferenceService
{
    /// <summary>
    /// Generates a completion for the given session and user message.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userMessage">The user's message.</param>
    /// <param name="options">Optional inference parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inference result.</returns>
    Task<InferenceResult> GenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a completion for the given session and user message.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userMessage">The user's message.</param>
    /// <param name="options">Optional inference parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of inference stream results.</returns>
    IAsyncEnumerable<InferenceStreamResult> StreamGenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion using a specific inference configuration.
    /// </summary>
    /// <param name="configId">The inference configuration identifier.</param>
    /// <param name="messages">The chat messages.</param>
    /// <param name="options">Optional inference parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inference result.</returns>
    Task<InferenceResult> GenerateWithConfigAsync(
        Guid configId,
        IReadOnlyList<ChatMessage> messages,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring inference generation.
/// </summary>
public sealed record InferenceOptions
{
    /// <summary>
    /// The sampling temperature, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    public double? Temperature { get; init; }

    /// <summary>
    /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass.
    /// </summary>
    public double? TopP { get; init; }

    /// <summary>
    /// The maximum number of tokens to generate.
    /// </param>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Whether to stream the response or return it all at once.
    /// </summary>
    public bool Stream { get; init; } = false;

    /// <summary>
    /// Optional retrieval context to include in the prompt.
    /// </summary>
    public IReadOnlyList<RetrievalContext>? RetrievalContext { get; init; }
}

/// <summary>
/// Represents a piece of retrieval context.
/// </summary>
public sealed record RetrievalContext
{
    /// <summary>
    /// The content of the retrieval context.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The source of the retrieval context (e.g., filename, document title).
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The relevance score of this context (0-1).
    /// </summary>
    public double RelevanceScore { get; init; }
}

/// <summary>
/// The result of an inference generation.
/// </summary>
public sealed record InferenceResult
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The number of tokens in the prompt.
    /// </summary>
    public required int PromptTokens { get; init; }

    /// <summary>
    /// The number of tokens in the completion.
    /// </summary>
    public required int CompletionTokens { get; init; }

    /// <summary>
    /// The total number of tokens (prompt + completion).
    /// </summary>
    public required int TotalTokens { get; init; }

    /// <summary>
    /// The model that generated the response.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The reason the generation stopped (e.g., "stop", "length").
    /// </summary>
    public required string FinishReason { get; init; }

    /// <summary>
    /// When the generation was completed.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A chunk of a streaming inference result.
/// </summary>
public sealed record InferenceStreamResult
{
    /// <summary>
    /// The delta content (new text since last chunk).
    /// </summary>
    public string? DeltaContent { get; init; }

    /// <summary>
    /// The finish reason (if the stream is complete).
    /// </summary>
    public string? FinishReason { get; init; }

    /// <summary>
    /// The number of prompt tokens in this chunk.
    /// </summary>
    public int? PromptTokens { get; init; }

    /// <summary>
    /// The number of completion tokens in this chunk.
    /// </summary>
    public int? CompletionTokens { get; init; }

    /// <summary>
    /// The total number of tokens in this chunk.
    /// </summary>
    public int? TotalTokens { get; init; }

    /// <summary>
    /// Whether this chunk represents the end of the stream.
    /// </summary>
    public bool IsComplete { get; init; }
}