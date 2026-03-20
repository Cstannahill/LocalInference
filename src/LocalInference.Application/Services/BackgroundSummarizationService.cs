using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Summarization;
using LocalInference.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalInference.Application.Services;

/// <summary>
/// Background service that monitors session context length and triggers summarization when threshold is reached.
/// </summary>
public class BackgroundSummarizationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundSummarizationService> _logger;
    private const double SummarizationThreshold = 0.85; // 85% of context limit

    public BackgroundSummarizationService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundSummarizationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background summarization service starting.");

        // In a real implementation, we would use a timer or channel to check sessions periodically
        // For simplicity, we'll just log that the service is ready
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check all active sessions for summarization needs
            await CheckSessionsForSummarizationAsync(stoppingToken);

            // Wait before next check (e.g., every 30 seconds)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CheckSessionsForSummarizationAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var summarizationService = scope.ServiceProvider.GetRequiredService<ITechnicalSummarizationService>();

        // Get all active sessions (simplified - in reality we'd have a more efficient way)
        var sessions = await sessionRepository.GetAllAsync(0, 100, stoppingToken);

        foreach (var session in sessions)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await CheckAndSummarizeSessionAsync(session.Id, sessionRepository, summarizationService, stoppingToken);
        }
    }

    private async Task CheckAndSummarizeSessionAsync(
        Guid sessionId,
        ISessionRepository sessionRepository,
        ITechnicalSummarizationService summarizationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null) return;

            // Get current token count (simplified)
            int currentTokenCount = await GetSessionTokenCountAsync(sessionId, sessionRepository, cancellationToken);

            // Get the system profile to determine max context tokens
            int maxContextTokens = await GetMaxContextTokensForSessionAsync(sessionId, sessionRepository, cancellationToken);

            double usageRatio = (double)currentTokenCount / maxContextTokens;

            if (usageRatio >= SummarizationThreshold)
            {
                _logger.LogInformation("Session {SessionId} exceeded summarization threshold ({UsageRatio:P2}). Triggering summarization.",
                    sessionId, usageRatio);

                // Trigger summarization - condense the oldest 50% of messages
                var messageSummaries = session.Messages.Select(m => new MessageSummary
                {
                    Role = m.Role.ToString(),
                    Content = m.Content,
                    Timestamp = m.CreatedAt
                }).ToList();

                await summarizationService.SummarizeConversationAsync(messageSummaries, new SummarizationOptions(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session {SessionId} for summarization", sessionId);
        }
    }

    private async Task<int> GetSessionTokenCountAsync(Guid sessionId, ISessionRepository sessionRepository, CancellationToken cancellationToken)
    {
        // In a real implementation, this would calculate actual tokens
        // For now, we'll return a placeholder based on message count
        var messages = await sessionRepository.GetMessagesAsync(sessionId, cancellationToken);
        // Rough estimate: 4 tokens per word, average 10 words per message
        return messages.Sum(m => m.Content.Length) / 4; // Very rough approximation
    }

    private async Task<int> GetMaxContextTokensForSessionAsync(Guid sessionId, ISessionRepository sessionRepository, CancellationToken cancellationToken)
    {
        // Get the session's system profile to determine max context tokens
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.SystemProfile == null)
            return 8192; // Default

        return session.SystemProfile.MaxContextTokens;
    }
}