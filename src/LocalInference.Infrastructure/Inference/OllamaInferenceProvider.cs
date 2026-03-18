using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalInference.Application.Abstractions.Inference;
using Microsoft.Extensions.Logging;

namespace LocalInference.Infrastructure.Inference;

public class OllamaInferenceProvider : IInferenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaInferenceProvider> _logger;

    public string ProviderName => "Ollama";

    public OllamaInferenceProvider(HttpClient httpClient, ILogger<OllamaInferenceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var ollamaRequest = new OllamaChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = request.Messages.Select(m => new OllamaMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                TopP = request.TopP,
                NumPredict = request.MaxTokens,
                Seed = request.Seed,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stop = request.StopSequences.ToList()
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            ollamaRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to parse Ollama response");
        }

        var promptTokens = result.PromptEvalCount ?? EstimateTokens(request.Messages);
        var completionTokens = result.EvalCount ?? EstimateTokens(result.Message?.Content ?? "");

        return new InferenceResponse
        {
            Id = Guid.NewGuid().ToString(),
            Model = request.ModelIdentifier,
            Content = result.Message?.Content ?? "",
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = promptTokens + completionTokens,
            CreatedAt = DateTime.UtcNow,
            FinishReason = result.Done ? "stop" : null
        };
    }

    public async IAsyncEnumerable<InferenceStreamChunk> StreamCompletionAsync(
        InferenceRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ollamaRequest = new OllamaChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = request.Messages.Select(m => new OllamaMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            Stream = true,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                TopP = request.TopP,
                NumPredict = request.MaxTokens,
                Seed = request.Seed,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stop = request.StopSequences.ToList()
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(ollamaRequest)
        };

        var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var id = Guid.NewGuid().ToString();
        var fullContent = new StringBuilder();
        int? promptTokens = null;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaStreamChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk == null) continue;

            if (chunk.Done)
            {
                yield return new InferenceStreamChunk
                {
                    Id = id,
                    Model = request.ModelIdentifier,
                    DeltaContent = null,
                    FinishReason = "stop",
                    PromptTokens = promptTokens ?? chunk.PromptEvalCount,
                    CompletionTokens = chunk.EvalCount,
                    TotalTokens = (promptTokens ?? chunk.PromptEvalCount ?? 0) + (chunk.EvalCount ?? 0)
                };
                yield break;
            }

            if (chunk.Message != null)
            {
                fullContent.Append(chunk.Message.Content);
                yield return new InferenceStreamChunk
                {
                    Id = id,
                    Model = request.ModelIdentifier,
                    DeltaContent = chunk.Message.Content,
                    PromptTokens = chunk.PromptEvalCount,
                    CompletionTokens = null,
                    TotalTokens = null
                };

                if (chunk.PromptEvalCount.HasValue)
                {
                    promptTokens = chunk.PromptEvalCount;
                }
            }
        }
    }

    public Task<int> EstimateTokenCountAsync(string text, string modelIdentifier, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EstimateTokens(text));
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private int EstimateTokens(string text)
    {
        return (int)(text.Length / 4.0);
    }

    private int EstimateTokens(IReadOnlyList<ChatMessage> messages)
    {
        return messages.Sum(m => EstimateTokens(m.Content));
    }

    private class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions Options { get; set; } = new();
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double TopP { get; set; }

        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; }

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("stop")]
        public List<string> Stop { get; set; } = new();
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }
    }

    private class OllamaStreamChunk
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }
    }
}
