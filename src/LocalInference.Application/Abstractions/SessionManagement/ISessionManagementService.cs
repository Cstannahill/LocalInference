using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace LocalInference.Application.Abstractions.SessionManagement
{
    /// <summary>
    /// Service for managing sessions.
    /// </summary>
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
}