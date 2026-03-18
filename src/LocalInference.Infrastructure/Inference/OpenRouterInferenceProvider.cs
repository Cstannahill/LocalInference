using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalInference.Application.Abstractions.Inference;
using Microsoft.Extensions.Logging;

namespace LocalInference.Infrastructure.Inference;

public class OpenRouterInferenceProvider : IInferenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterInferenceProvider> _logger;

    public string ProviderName => "OpenRouter";

    public OpenRouterInferenceProvider(HttpClient httpClient, ILogger<OpenRouterInferenceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var openRouterRequest = new OpenRouterChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = request.Messages.Select(m => new OpenRouterMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name
            }).ToList(),
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens,
            Seed = request.Seed,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            Stop = request.StopSequences.ToList()
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/chat/completions",
            openRouterRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(
            cancellationToken: cancellationToken);

        if (result?.Choices == null || result.Choices.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse OpenRouter response");
        }

        var choice = result.Choices[0];

        return new InferenceResponse
        {
            Id = result.Id ?? Guid.NewGuid().ToString(),
            Model = result.Model ?? request.ModelIdentifier,
            Content = choice.Message?.Content ?? "",
            PromptTokens = result.Usage?.PromptTokens ?? 0,
            CompletionTokens = result.Usage?.CompletionTokens ?? 0,
            TotalTokens = result.Usage?.TotalTokens ?? 0,
            CreatedAt = DateTime.UtcNow,
            FinishReason = choice.FinishReason
        };
    }

    public async IAsyncEnumerable<InferenceStreamChunk> StreamCompletionAsync(
        InferenceRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openRouterRequest = new OpenRouterChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = request.Messages.Select(m => new OpenRouterMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name
            }).ToList(),
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens,
            Stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/chat/completions")
        {
            Content = JsonContent.Create(openRouterRequest)
        };

        var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var id = Guid.NewGuid().ToString();

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line.Substring(6);
            if (data == "[DONE]") yield break;

            OpenRouterStreamChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenRouterStreamChunk>(data);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk?.Choices == null || chunk.Choices.Count == 0) continue;

            var choice = chunk.Choices[0];

            yield return new InferenceStreamChunk
            {
                Id = chunk.Id ?? id,
                Model = chunk.Model ?? request.ModelIdentifier,
                DeltaContent = choice.Delta?.Content,
                FinishReason = choice.FinishReason,
                PromptTokens = chunk.Usage?.PromptTokens,
                CompletionTokens = chunk.Usage?.CompletionTokens,
                TotalTokens = chunk.Usage?.TotalTokens
            };

            if (choice.FinishReason != null) yield break;
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
            var response = await _httpClient.GetAsync("/api/v1/models", cancellationToken);
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

    private class OpenRouterChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double TopP { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("stop")]
        public List<string> Stop { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OpenRouterMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class OpenRouterChatResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenRouterUsage? Usage { get; set; }
    }

    private class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class OpenRouterUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class OpenRouterStreamChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<OpenRouterStreamChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenRouterUsage? Usage { get; set; }
    }

    private class OpenRouterStreamChoice
    {
        [JsonPropertyName("delta")]
        public OpenRouterDelta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class OpenRouterDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
