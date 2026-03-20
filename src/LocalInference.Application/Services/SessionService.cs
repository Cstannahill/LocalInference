using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;
using LocalInference.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LocalInference.Application.Services;

public class InferenceService : IInferenceService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IInferenceConfigRepository _configRepository;
    private readonly IInferenceProviderFactory _providerFactory;
    private readonly IContextManager _contextManager;
    private readonly ILogger<InferenceService> _logger;

    public InferenceService(
        ISessionRepository sessionRepository,
        IInferenceConfigRepository configRepository,
        IInferenceProviderFactory providerFactory,
        IContextManager contextManager,
        ILogger<InferenceService> logger)
    {
        _sessionRepository = sessionRepository;
        _configRepository = configRepository;
        _providerFactory = providerFactory;
        _contextManager = contextManager;
        _logger = logger;
    }

    public async Task<InferenceResult> GenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new SessionNotFoundException(sessionId);

        var messages = await BuildMessagesAsync(session, userMessage, options?.RetrievalContext, cancellationToken);
        var provider = _providerFactory.GetProvider(session.InferenceConfig.ProviderType);

        var request = CreateRequest(session.InferenceConfig, messages, options);
        var response = await provider.CompleteAsync(request, cancellationToken);

        await SaveMessagesAsync(session, userMessage, response.Content, cancellationToken);

        return new InferenceResult
        {
            Content = response.Content,
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            TotalTokens = response.TotalTokens,
            Model = response.Model,
            FinishReason = response.FinishReason ?? "stop"
        };
    }

    public async IAsyncEnumerable<InferenceStreamResult> StreamGenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new SessionNotFoundException(sessionId);

        var messages = await BuildMessagesAsync(session, userMessage, options?.RetrievalContext, cancellationToken);
        var provider = _providerFactory.GetProvider(session.InferenceConfig.ProviderType);

        var request = CreateRequest(session.InferenceConfig, messages, options) with { Stream = true };

        var fullContent = new System.Text.StringBuilder();
        await foreach (var chunk in provider.StreamCompletionAsync(request, cancellationToken))
        {
            if (chunk.DeltaContent != null)
            {
                fullContent.Append(chunk.DeltaContent);
            }

            yield return new InferenceStreamResult
            {
                DeltaContent = chunk.DeltaContent,
                FinishReason = chunk.FinishReason,
                PromptTokens = chunk.PromptTokens,
                CompletionTokens = chunk.CompletionTokens,
                TotalTokens = chunk.TotalTokens,
                IsComplete = chunk.FinishReason != null
            };
        }

        await SaveMessagesAsync(session, userMessage, fullContent.ToString(), cancellationToken);
    }

    public async Task<InferenceResult> GenerateWithConfigAsync(
        Guid configId,
        IReadOnlyList<ChatMessage> messages,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.GetByIdAsync(configId, cancellationToken)
            ?? throw new InferenceConfigNotFoundException(configId);

        var provider = _providerFactory.GetProvider(config.ProviderType);
        var request = CreateRequest(config, messages, options);

        var response = await provider.CompleteAsync(request, cancellationToken);

        return new InferenceResult
        {
            Content = response.Content,
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            TotalTokens = response.TotalTokens,
            Model = response.Model,
            FinishReason = response.FinishReason ?? "stop"
        };
    }

    private async Task<IReadOnlyList<ChatMessage>> BuildMessagesAsync(
        Session session,
        string userMessage,
        IReadOnlyList<RetrievalContext>? retrievalContext,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(session.InferenceConfig.SystemPrompt))
        {
            messages.Add(ChatMessage.System(session.InferenceConfig.SystemPrompt));
        }

        if (retrievalContext?.Count > 0)
        {
            var contextContent = string.Join("\n\n", retrievalContext.Select(c => $"[{c.Source}]\n{c.Content}"));
            messages.Add(ChatMessage.System($"Relevant context:\n{contextContent}"));
        }

        var contextMessages = await _contextManager.GetOptimizedContextAsync(session.Id, userMessage, cancellationToken);
        foreach (var msg in contextMessages)
        {
            messages.Add(new ChatMessage
            {
                Role = msg.Role.ToString().ToLower(),
                Content = msg.Content
            });
        }

        messages.Add(ChatMessage.User(userMessage));

        return messages;
    }

    private InferenceRequest CreateRequest(InferenceConfig config, IReadOnlyList<ChatMessage> messages, InferenceOptions? options)
    {
        return new InferenceRequest
        {
            ModelIdentifier = config.ModelIdentifier,
            Messages = messages,
            Temperature = options?.Temperature ?? config.Temperature,
            TopP = options?.TopP ?? config.TopP,
            MaxTokens = options?.MaxTokens ?? config.MaxTokens ?? 2048,
            Seed = config.Seed,
            FrequencyPenalty = config.FrequencyPenalty,
            PresencePenalty = config.PresencePenalty,
            StopSequences = config.StopSequences?.Split(',').Select(s => s.Trim()).ToArray() ?? Array.Empty<string>(),
            Stream = options?.Stream ?? false
        };
    }

    private async Task SaveMessagesAsync(Session session, string userMessage, string assistantMessage, CancellationToken cancellationToken)
    {
        // Reload the session fresh to avoid entity state tracking issues
        // This ensures new messages are properly recognized as "Added", not "Modified"
        var freshSession = await _sessionRepository.GetByIdWithMessagesAsync(session.Id, cancellationToken)
            ?? throw new SessionNotFoundException(session.Id);
        
        freshSession.AddMessage(MessageRole.User, userMessage, EstimateTokens(userMessage));
        freshSession.AddMessage(MessageRole.Assistant, assistantMessage, EstimateTokens(assistantMessage));
        await _sessionRepository.UpdateAsync(freshSession, cancellationToken);
    }

    private int EstimateTokens(string text)
    {
        return (int)(text.Length / 4.0);
    }
}
