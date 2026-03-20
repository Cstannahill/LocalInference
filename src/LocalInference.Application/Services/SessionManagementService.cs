using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LocalInference.Application.Services;

public interface ISessionManagementService
{
    Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<SessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(ListSessionsRequest request, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task ClearSessionContextAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed record CreateSessionRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? InferenceConfigId { get; init; }
    public int? ContextWindowTokens { get; init; }
    public int? MaxOutputTokens { get; init; }
}

public sealed record UpdateSessionRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Guid? InferenceConfigId { get; init; }
    public int? ContextWindowTokens { get; init; }
}

public sealed record ListSessionsRequest
{
    public bool ActiveOnly { get; init; } = false;
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 100;
}

public sealed record SessionDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Guid InferenceConfigId { get; init; }
    public required string InferenceConfigName { get; init; }
    public required int ContextWindowTokens { get; init; }
    public required int MaxOutputTokens { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public int MessageCount { get; init; }
    public int TotalTokenCount { get; init; }
    public Guid? SystemProfileId { get; init; }
    public string? SystemProfileName { get; init; }
}

public sealed record SessionSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string InferenceConfigName { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public int MessageCount { get; init; }
}

public sealed record SessionStatistics
{
    public required int TotalMessages { get; init; }
    public required int TotalTokens { get; init; }
    public required int AverageMessageLength { get; init; }
    public required int CheckpointCount { get; init; }
    public required double CompressionRatio { get; init; }
    public required DateTime FirstMessageAt { get; init; }
    public required DateTime LastMessageAt { get; init; }
}

public class SessionManagementService : ISessionManagementService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IInferenceConfigRepository _configRepository;
    private readonly ILogger<SessionManagementService> _logger;

    public SessionManagementService(
        ISessionRepository sessionRepository,
        IInferenceConfigRepository configRepository,
        ILogger<SessionManagementService> logger)
    {
        _sessionRepository = sessionRepository;
        _configRepository = configRepository;
        _logger = logger;
    }

    public async Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        InferenceConfig config;

        if (request.InferenceConfigId.HasValue)
        {
            config = await _configRepository.GetByIdAsync(request.InferenceConfigId.Value, cancellationToken)
                ?? throw new InferenceConfigNotFoundException(request.InferenceConfigId.Value);
        }
        else
        {
            config = await _configRepository.GetDefaultAsync(cancellationToken)
                ?? throw new DomainException("No default inference configuration found");
        }

        var session = Session.Create(
            request.Name,
            config,
            null,
            request.ContextWindowTokens ?? 8192,
            request.MaxOutputTokens ?? 2048
        );

        await _sessionRepository.AddAsync(session, cancellationToken);

        return MapToDto(session, config);
    }

    public async Task<SessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken);
        return session == null ? null : MapToDto(session);
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(ListSessionsRequest request, CancellationToken cancellationToken = default)
    {
        var sessions = request.ActiveOnly
            ? await _sessionRepository.GetActiveSessionsAsync(cancellationToken)
            : await _sessionRepository.GetAllAsync(request.Skip, request.Take, cancellationToken);

        return sessions.Select(MapToSummaryDto).ToList();
    }

    public async Task UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new SessionNotFoundException(sessionId);

        if (request.Name != null)
        {
            session.UpdateName(request.Name);
        }

        if (request.Description != null)
        {
            session.UpdateDescription(request.Description);
        }

        if (request.InferenceConfigId.HasValue)
        {
            var config = await _configRepository.GetByIdAsync(request.InferenceConfigId.Value, cancellationToken)
                ?? throw new InferenceConfigNotFoundException(request.InferenceConfigId.Value);

            session.UpdateInferenceConfig(config.Id, config);
        }

        if (request.ContextWindowTokens.HasValue)
        {
            session.UpdateContextWindowTokens(request.ContextWindowTokens.Value);
        }

        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.DeleteAsync(sessionId, cancellationToken);
    }

    public async Task ClearSessionContextAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new SessionNotFoundException(sessionId);

        session.Deactivate();
        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithMessagesAsync(sessionId, cancellationToken)
            ?? throw new SessionNotFoundException(sessionId);

        var messages = session.Messages.ToList();
        var checkpoints = session.Checkpoints.ToList();

        var totalTokens = messages.Sum(m => m.TokenCount);
        var originalTokens = checkpoints.Sum(c => c.OriginalTokenCount);
        var compressedTokens = checkpoints.Sum(c => c.CompressedTokenCount);

        return new SessionStatistics
        {
            TotalMessages = messages.Count,
            TotalTokens = totalTokens,
            AverageMessageLength = messages.Count > 0 ? (int)messages.Average(m => m.Content.Length) : 0,
            CheckpointCount = checkpoints.Count,
            CompressionRatio = originalTokens > 0 ? (double)compressedTokens / originalTokens : 0,
            FirstMessageAt = messages.MinBy(m => m.CreatedAt)?.CreatedAt ?? session.CreatedAt,
            LastMessageAt = messages.MaxBy(m => m.CreatedAt)?.CreatedAt ?? session.CreatedAt
        };
    }

    private SessionDto MapToDto(Session session, InferenceConfig? config = null)
    {
        var configName = config?.Name ?? session.InferenceConfig?.Name ?? "Unknown";
        var systemProfileName = session.SystemProfile?.Name;

        return new SessionDto
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            InferenceConfigId = session.InferenceConfigId,
            InferenceConfigName = configName,
            ContextWindowTokens = session.ContextWindowTokens,
            MaxOutputTokens = session.MaxOutputTokens,
            IsActive = session.IsActive,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            LastActivityAt = session.LastActivityAt,
            MessageCount = session.Messages.Count,
            TotalTokenCount = session.GetTotalTokenCount(),
            SystemProfileId = session.SystemProfileId,
            SystemProfileName = systemProfileName
        };
    }

    private SessionSummaryDto MapToSummaryDto(Session session)
    {
        return new SessionSummaryDto
        {
            Id = session.Id,
            Name = session.Name,
            InferenceConfigName = session.InferenceConfig.Name,
            IsActive = session.IsActive,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            MessageCount = session.Messages.Count
        };
    }
}
