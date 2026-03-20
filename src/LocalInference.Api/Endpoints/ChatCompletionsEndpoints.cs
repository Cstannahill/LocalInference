using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Prompting;
using LocalInference.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LocalInference.Api.Endpoints;

public static class ChatCompletionsEndpoints
{
    public static IEndpointRouteBuilder MapChatCompletionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1").WithTags("Chat Completions");

        group.MapPost("/chat/completions", async (
            [FromBody] ChatCompletionRequest request,
            IInferenceService inferenceService,
            ISessionManagementService sessionService,
            IContextComposer contextComposer,
            IRepository<SystemProfile> systemProfileRepository,
            CancellationToken cancellationToken) =>
        {
            Guid sessionId;
            if (!string.IsNullOrEmpty(request.SessionId) && Guid.TryParse(request.SessionId, out var parsedSessionId))
            {
                sessionId = parsedSessionId;
            }
            else
            {
                // Determine which system profile to use (default to first available or a default one)
                var defaultProfile = await systemProfileRepository.GetAllAsync(cancellationToken);
                Guid? profileId = defaultProfile.FirstOrDefault()?.Id;

                var session = await sessionService.CreateSessionAsync(new CreateSessionRequest
                {
                    Name = $"Chat {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    InferenceConfigId = request.ConfigId,
                    SystemProfileId = profileId // Link to system profile if available
                }, cancellationToken);
                sessionId = session.Id;
            }

            var userMessage = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";

            // Get the system profile for context composition
            var systemProfile = await systemProfileRepository.GetByIdAsync(
                sessionId.HasValue ? sessionId.Value : Guid.Empty,
                cancellationToken);

            if (request.Stream)
            {
                async Task StreamResponse(Stream stream)
                {
                    var id = $"chatcmpl-{Guid.NewGuid():N}";
                    var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {System.Text.Json.JsonSerializer.Serialize(new ChatCompletionStreamResponse
                    {
                        Id = id,
                        Object = "chat.completion.chunk",
                        Created = created,
                        Model = request.Model ?? "default",
                        Choices = new[] { new ChatCompletionStreamChoice { Delta = new DeltaMessage { Role = "assistant" } } }
                    })}\n\n"));

                    // For streaming, we still need to compose the context but send it in chunks
                    var context = await contextComposer.ComposePromptAsync(sessionId, userMessage, cancellationToken);

                    // In a real implementation, we would stream the LLM response with the pre-composed context
                    // For now, we'll simulate by sending the context as the first chunk
                    await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {System.Text.Json.JsonSerializer.Serialize(new ChatCompletionStreamResponse
                    {
                        Id = id,
                        Object = "chat.completion.chunk",
                        Created = created,
                        Model = request.Model ?? "default",
                        Choices = new[] { new ChatCompletionStreamChoice { Delta = new DeltaMessage { Content = context } } }
                    })}\n\n"));

                    await foreach (var chunk in inferenceService.StreamGenerateAsync(sessionId, userMessage, new InferenceOptions
                    {
                        Temperature = (double?)request.Temperature,
                        TopP = (double?)request.TopP,
                        MaxTokens = request.MaxTokens,
                        Stream = true
                    }, cancellationToken))
                    {
                        if (chunk.DeltaContent != null)
                        {
                            var streamChunk = new ChatCompletionStreamResponse
                            {
                                Id = id,
                                Object = "chat.completion.chunk",
                                Created = created,
                                Model = request.Model ?? "default",
                                Choices = new[]
                                {
                                    new ChatCompletionStreamChoice
                                    {
                                        Delta = new DeltaMessage { Content = chunk.DeltaContent },
                                        FinishReason = chunk.FinishReason
                                    }
                                }
                            };

                            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {System.Text.Json.JsonSerializer.Serialize(streamChunk)}\n\n"));
                        }

                        if (chunk.IsComplete)
                        {
                            var finalChunk = new ChatCompletionStreamResponse
                            {
                                Id = id,
                                Object = "chat.completion.chunk",
                                Created = created,
                                Model = request.Model ?? "default",
                                Choices = new[]
                                {
                                    new ChatCompletionStreamChoice
                                    {
                                        Delta = new DeltaMessage(),
                                        FinishReason = chunk.FinishReason ?? "stop"
                                    }
                                }
                            };

                            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {System.Text.Json.JsonSerializer.Serialize(finalChunk)}\n\n"));
                            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("data: [DONE]\n\n"));
                            break;
                        }
                    }
                }

                return Results.Stream(StreamResponse, "text/event-stream");
            }

            // Non-streaming path - compose the full context and send to inference
            var context = await contextComposer.ComposePromptAsync(sessionId, userMessage, cancellationToken);

            var result = await inferenceService.GenerateAsync(sessionId, context, new InferenceOptions
            {
                Temperature = (double?)request.Temperature,
                TopP = (double?)request.TopP,
                MaxTokens = request.MaxTokens
            }, cancellationToken);

            var response = new ChatCompletionResponse
            {
                Id = $"chatcmpl-{Guid.NewGuid():N}",
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = request.Model ?? "default",
                SessionId = sessionId.ToString(),
                Choices = new[]
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new ChatMessageResponse
                        {
                            Role = "assistant",
                            Content = result.Content
                        },
                        FinishReason = result.FinishReason
                    }
                },
                Usage = new UsageInfo
                {
                    PromptTokens = result.PromptTokens,
                    CompletionTokens = result.CompletionTokens,
                    TotalTokens = result.TotalTokens
                }
            };

            return Results.Ok(response);
        })
        .WithName("CreateChatCompletion")
        .WithOpenApi()
        .Produces<ChatCompletionResponse>(200)
        .Produces(400);

        group.MapPost("/inference", async (
            [FromBody] InferenceRequestDto request,
            IInferenceService inferenceService,
            CancellationToken cancellationToken) =>
        {
            var messages = request.Messages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            var result = await inferenceService.GenerateWithConfigAsync(
                request.ConfigId,
                messages,
                new InferenceOptions
                {
                    Temperature = request.Temperature,
                    TopP = request.TopP,
                    MaxTokens = request.MaxTokens
                },
                cancellationToken);

            return Results.Ok(new InferenceResponseDto
            {
                Content = result.Content,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TotalTokens,
                Model = result.Model,
                FinishReason = result.FinishReason
            });
        })
        .WithName("CreateInference")
        .WithOpenApi()
        .Produces<InferenceResponseDto>(200);

        return app;
    }
}

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<ChatMessageRequest> Messages { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("config_id")]
    public Guid? ConfigId { get; set; }
}

public sealed class ChatMessageRequest
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; set; }

    [JsonPropertyName("choices")]
    public required ChatCompletionChoice[] Choices { get; set; }

    [JsonPropertyName("usage")]
    public required UsageInfo Usage { get; set; }
}

public sealed class ChatCompletionChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public required ChatMessageResponse Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public required string FinishReason { get; set; }
}

public sealed class ChatMessageResponse
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public sealed class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public sealed class ChatCompletionStreamResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("choices")]
    public required ChatCompletionStreamChoice[] Choices { get; set; }
}

public sealed class ChatCompletionStreamChoice
{
    [JsonPropertyName("delta")]
    public required DeltaMessage Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class DeltaMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public sealed class InferenceRequestDto
{
    public Guid ConfigId { get; set; }
    public required List<MessageDto> Messages { get; set; }
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
}

public sealed class MessageDto
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public sealed class InferenceResponseDto
{
    public required string Content { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public required string Model { get; set; }
    public required string FinishReason { get; set; }
}
