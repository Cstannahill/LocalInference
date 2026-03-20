using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Summarization;
using LocalInference.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalInference.Application.Jobs;

/// <summary>
/// Job that performs conversation summarization when triggered by the background service.
/// </summary>
public class SummarizationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SummarizationJob> _logger;

    public SummarizationJob(
        IServiceProvider serviceProvider,
        ILogger<SummarizationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Summarization job starting.");

        // In a real implementation, we would use a timer or channel to process queued jobs
        // For simplicity, we'll just log that the service is ready
        while (!stoppingToken.IsCancellationRequested)
        {
            // Process summarization queue (placeholder)
            await ProcessSummarizationQueueAsync(stoppingToken);

            // Wait before next check
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessSummarizationQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var summarizationService = scope.ServiceProvider.GetRequiredService<ITechnicalSummarizationService>();
        var inferenceService = scope.ServiceProvider.GetRequiredService<IInferenceService>();

        // Get sessions that need summarization (in a real app, this would come from a queue)
        var sessions = await sessionRepository.GetAllAsync(0, 100, stoppingToken);

        foreach (var session in sessions)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await ProcessSessionSummarizationAsync(session.Id, sessionRepository, summarizationService, inferenceService, stoppingToken);
        }
    }

    private async Task ProcessSessionSummarizationAsync(
        Guid sessionId,
        ISessionRepository sessionRepository,
        ITechnicalSummarizationService summarizationService,
        IInferenceService inferenceService,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null) return;

            // Perform the summarization
            var messageSummaries = session.Messages.Select(m => new MessageSummary
            {
                Role = m.Role.ToString(),
                Content = m.Content,
                Timestamp = m.CreatedAt
            }).ToList();

            await summarizationService.SummarizeConversationAsync(messageSummaries, new SummarizationOptions(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing summarization for session {SessionId}", sessionId);
        }
    }
}