using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Summarization;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;
using LocalInference.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LocalInference.Application.Services;

public interface IContextManager
{
    Task<IReadOnlyList<ContextMessageDto>> GetOptimizedContextAsync(
        Guid sessionId,
        string currentMessage,
        CancellationToken cancellationToken = default);

    Task<ContextWindowState> GetContextStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task CompressContextAsync(
        Guid sessionId,
        CompressionStrategy strategy,
        CancellationToken cancellationToken = default);

    Task TrimContextAsync(
        Guid sessionId,
        int targetTokenCount,
        CancellationToken cancellationToken = default);
}

public sealed record ContextMessageDto
{
    public required MessageRole Role { get; init; }
    public required string Content { get; init; }
    public int TokenCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsSummarized { get; init; }
}

public enum CompressionStrategy
{
    SummarizeOldest,
    RemoveOldest,
    SlidingWindow,
    SmartCompression
}

public class ContextManager : IContextManager
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITechnicalSummarizationService _summarizationService;
    private readonly ILogger<ContextManager> _logger;

    private const int DEFAULT_CONTEXT_WINDOW = 8192;
    private const int RESERVE_FOR_OUTPUT = 2048;
    private const int RESERVE_FOR_SYSTEM = 512;
    private const int SUMMARY_THRESHOLD_MESSAGES = 10;
    private const double COMPRESSION_TARGET_RATIO = 0.3;

    public ContextManager(
        ISessionRepository sessionRepository,
        ITechnicalSummarizationService summarizationService,
        ILogger<ContextManager> logger)
    {
        _sessionRepository = sessionRepository;
        _summarizationService = summarizationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ContextMessageDto>> GetOptimizedContextAsync(
        Guid sessionId,
        string currentMessage,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return Array.Empty<ContextMessageDto>();
        }

        var availableTokens = session.ContextWindowTokens - RESERVE_FOR_OUTPUT - RESERVE_FOR_SYSTEM;
        var messages = session.Messages.ToList();
        var checkpoints = session.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.StartMessageIndex).ToList();

        var result = new List<ContextMessageDto>();
        var currentTokens = 0;

        foreach (var checkpoint in checkpoints)
        {
            var summaryTokens = EstimateTokens(checkpoint.Summary);
            if (currentTokens + summaryTokens <= availableTokens * 0.4)
            {
                result.Add(new ContextMessageDto
                {
                    Role = MessageRole.System,
                    Content = $"Previous conversation summary: {checkpoint.Summary}",
                    TokenCount = summaryTokens,
                    CreatedAt = checkpoint.CreatedAt,
                    IsSummarized = true
                });
                currentTokens += summaryTokens;
            }
        }

        var recentMessages = messages.Where(m => !m.IsSummarized).OrderBy(m => m.SequenceNumber).ToList();
        var messagesToInclude = new List<ContextMessage>();

        for (int i = recentMessages.Count - 1; i >= 0; i--)
        {
            var message = recentMessages[i];
            if (currentTokens + message.TokenCount <= availableTokens)
            {
                messagesToInclude.Insert(0, message);
                currentTokens += message.TokenCount;
            }
            else
            {
                break;
            }
        }

        foreach (var message in messagesToInclude)
        {
            result.Add(new ContextMessageDto
            {
                Role = message.Role,
                Content = message.Content,
                TokenCount = message.TokenCount,
                CreatedAt = message.CreatedAt,
                IsSummarized = false
            });
        }

        return result;
    }

    public async Task<ContextWindowState> GetContextStateAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return ContextWindowState.Calculate(0, DEFAULT_CONTEXT_WINDOW, 0, 0);
        }

        var totalTokens = session.GetTotalTokenCount();
        var systemTokens = !string.IsNullOrEmpty(session.InferenceConfig.SystemPrompt)
            ? EstimateTokens(session.InferenceConfig.SystemPrompt)
            : 0;

        return ContextWindowState.Calculate(totalTokens, session.ContextWindowTokens, systemTokens, RESERVE_FOR_OUTPUT);
    }

    public async Task CompressContextAsync(
        Guid sessionId,
        CompressionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken);
        if (session == null) return;

        switch (strategy)
        {
            case CompressionStrategy.SummarizeOldest:
                await SummarizeOldestMessagesAsync(session, cancellationToken);
                break;
            case CompressionStrategy.RemoveOldest:
                await RemoveOldestMessagesAsync(session, cancellationToken);
                break;
            case CompressionStrategy.SlidingWindow:
                await ApplySlidingWindowAsync(session, cancellationToken);
                break;
            case CompressionStrategy.SmartCompression:
                await ApplySmartCompressionAsync(session, cancellationToken);
                break;
        }

        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task TrimContextAsync(
        Guid sessionId,
        int targetTokenCount,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken);
        if (session == null) return;

        var messages = session.Messages.OrderByDescending(m => m.SequenceNumber).ToList();
        var currentTokens = session.GetTotalTokenCount();

        while (currentTokens > targetTokenCount && messages.Count > 2)
        {
            var oldestMessage = messages.Last();
            currentTokens -= oldestMessage.TokenCount;
            messages.RemoveAt(messages.Count - 1);
        }

        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    private async Task SummarizeOldestMessagesAsync(Session session, CancellationToken cancellationToken)
    {
        var messagesToSummarize = session.Messages
            .Where(m => !m.IsSummarized)
            .OrderBy(m => m.SequenceNumber)
            .Take(SUMMARY_THRESHOLD_MESSAGES)
            .ToList();

        if (messagesToSummarize.Count < 3) return;

        var messageSummaries = messagesToSummarize.Select(m => new MessageSummary
        {
            Role = m.Role.ToString(),
            Content = m.Content,
            Timestamp = m.CreatedAt
        }).ToList();

        var options = new SummarizationOptions
        {
            TargetTokenCount = (int)(messagesToSummarize.Sum(m => m.TokenCount) * COMPRESSION_TARGET_RATIO),
            PreserveKeyFacts = true,
            PreserveTechnicalDetails = true
        };

        var summary = await _summarizationService.SummarizeConversationAsync(messageSummaries, options, cancellationToken);
        var summaryTokens = EstimateTokens(summary);

        var startIndex = messagesToSummarize.First().SequenceNumber;
        var endIndex = messagesToSummarize.Last().SequenceNumber;

        session.AddCheckpoint(startIndex, endIndex, summary, summaryTokens);

        foreach (var message in messagesToSummarize)
        {
            message.MarkAsSummarized(session.Checkpoints.Last().Id);
        }
    }

    private async Task RemoveOldestMessagesAsync(Session session, CancellationToken cancellationToken)
    {
        var messagesToRemove = session.Messages
            .Where(m => !m.IsSummarized)
            .OrderBy(m => m.SequenceNumber)
            .Take(SUMMARY_THRESHOLD_MESSAGES / 2)
            .ToList();

        foreach (var message in messagesToRemove)
        {
            message.MarkAsSummarized(Guid.Empty);
        }
    }

    private async Task ApplySlidingWindowAsync(Session session, CancellationToken cancellationToken)
    {
        var availableTokens = session.ContextWindowTokens - RESERVE_FOR_OUTPUT - RESERVE_FOR_SYSTEM;
        var messages = session.Messages.OrderBy(m => m.SequenceNumber).ToList();

        var currentTokens = 0;
        var firstIncludedIndex = messages.Count;

        for (int i = messages.Count - 1; i >= 0; i--)
        {
            if (currentTokens + messages[i].TokenCount <= availableTokens)
            {
                currentTokens += messages[i].TokenCount;
                firstIncludedIndex = i;
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < firstIncludedIndex; i++)
        {
            if (!messages[i].IsSummarized)
            {
                messages[i].MarkAsSummarized(Guid.Empty);
            }
        }
    }

    private async Task ApplySmartCompressionAsync(Session session, CancellationToken cancellationToken)
    {
        var state = await GetContextStateAsync(session.Id, cancellationToken);

        if (state.UtilizationRatio > 0.9)
        {
            await SummarizeOldestMessagesAsync(session, cancellationToken);
        }
        else if (state.UtilizationRatio > 0.75)
        {
            await ApplySlidingWindowAsync(session, cancellationToken);
        }
    }

    private int EstimateTokens(string text)
    {
        return (int)(text.Length / 4.0);
    }
}
