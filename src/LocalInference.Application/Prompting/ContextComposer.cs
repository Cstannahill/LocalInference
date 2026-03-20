using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Retrieval;
using LocalInference.Application.Abstractions.Summarization;
using LocalInference.Domain.Entities;
using LocalInference.Domain.ValueObjects;

namespace LocalInference.Application.Prompting;

/// <summary>
/// Composes the final prompt for the LLM by allocating tokens across different context slices
/// based on priority and availability.
/// </summary>
public class ContextComposer
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IReferenceDataRepository _referenceDataRepository;
    private readonly IExtractedKnowledgeRepository _extractedKnowledgeRepository;
    private readonly ITechnicalSummarizationService _summarizationService;
    private readonly IInferenceService _inferenceService;
    private readonly ITechnicalRetrievalService _retrievalService;

    public ContextComposer(
        ISessionRepository sessionRepository,
        IReferenceDataRepository referenceDataRepository,
        IExtractedKnowledgeRepository extractedKnowledgeRepository,
        ITechnicalSummarizationService summarizationService,
        IInferenceService inferenceService,
        ITechnicalRetrievalService retrievalService)
    {
        _sessionRepository = sessionRepository;
        _referenceDataRepository = referenceDataRepository;
        _extractedKnowledgeRepository = extractedKnowledgeRepository;
        _summarizationService = summarizationService;
        _inferenceService = inferenceService;
        _retrievalService = retrievalService;
    }

    /// <summary>
    /// Composes the prompt for a given session and user message.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The composed prompt ready for LLM inference.</returns>
    public async Task<string> ComposePromptAsync(Guid sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        // Get the session and its messages
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found.");

        // Get the system profile for this session
        var systemProfile = await GetSystemProfileForSessionAsync(sessionId, cancellationToken);

        // Get recent messages from the session
        var recentMessages = await GetRecentMessagesAsync(sessionId, cancellationToken);

        // Get relevant context from reference data and extracted knowledge
        var relevantContext = await GetRelevantContextAsync(sessionId, userMessage, cancellationToken);

        // Get summary if available
        var summary = await GetSessionSummaryAsync(sessionId, cancellationToken);

        // Compose the final prompt using the budget allocation
        return ComposePrompt(systemProfile, recentMessages, relevantContext, summary, userMessage);
    }

    private async Task<SystemProfile?> GetSystemProfileForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // This would typically join through a session-to-profile mapping
        // For now, we'll return a default profile or fetch from session
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        return session?.SystemProfile; // Assuming Session has a SystemProfile navigation property
    }

    private async Task<IReadOnlyList<ContextMessage>> GetRecentMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Get recent messages (e.g., last 10 turns or based on some limit)
        // This is simplified - in reality we'd want to get messages efficiently
        var messages = await _sessionRepository.GetMessagesAsync(sessionId, cancellationToken);
        // Return last N messages (e.g., 10 turns = 20 messages if alternating)
        return messages.TakeLast(Math.Min(20, messages.Count)).ToList();
    }

    private async Task<string> GetRelevantContextAsync(Guid sessionId, string userQuery, CancellationToken cancellationToken)
    {
        // Get relevant reference data chunks
        var referenceContext = await _retrievalService.RetrieveRelevantReferenceDataAsync(sessionId, userQuery, cancellationToken);

        // Get relevant extracted knowledge (long-term memory)
        var knowledgeContext = await _extractedKnowledgeRepository.GetRelevantKnowledgeAsync(sessionId, userQuery, cancellationToken);

        // Combine and format the context
        var contextParts = new List<string>();
        if (!string.IsNullOrEmpty(referenceContext))
            contextParts.Add($"Reference Context:\n{referenceContext}");

        if (!string.IsNullOrEmpty(knowledgeContext))
            contextParts.Add($"Knowledge Context:\n{knowledgeContext}");

        return string.Join("\n\n", contextParts);
    }

    private async Task<string> GetSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var checkpoint = await _sessionRepository.GetLatestCheckpointAsync(sessionId, cancellationToken);
        return checkpoint?.Summary ?? string.Empty;
    }

    private string ComposePrompt(SystemProfile? systemProfile,
                                IReadOnlyList<ContextMessage> recentMessages,
                                string relevantContext,
                                string summary,
                                string userMessage)
    {
        var promptParts = new List<string>();

        // System slice (high priority)
        if (systemProfile != null && !string.IsNullOrEmpty(systemProfile.SystemPrompt))
        {
            promptParts.Add($"System Instructions:\n{systemProfile.SystemPrompt}");
        }

        // Retrieval slice (medium priority) - reference data and extracted knowledge
        if (!string.IsNullOrEmpty(relevantContext))
        {
            promptParts.Add(relevantContext);
        }

        // Summary slice (medium priority) - condensed history
        if (!string.IsNullOrEmpty(summary))
        {
            promptParts.Add($"Previous Conversation Summary:\n{summary}");
        }

        // History slice (medium priority) - recent messages
        if (recentMessages.Any())
        {
            var historyText = string.Join("\n", recentMessages.Select(m =>
                $"{m.Role}: {m.Content}"));
            promptParts.Add($"Recent Conversation:\n{historyText}");
        }

        // Current user message (always included)
        promptParts.Add($"User: {userMessage}");
        promptParts.Add("Assistant:");

        return string.Join("\n\n", promptParts);
    }
}