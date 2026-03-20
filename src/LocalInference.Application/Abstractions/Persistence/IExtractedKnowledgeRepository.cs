using LocalInference.Domain.Entities;

namespace LocalInference.Application.Abstractions.Persistence;

/// <summary>
/// Repository for managing extracted knowledge (long-term memory).
/// </summary>
public interface IExtractedKnowledgeRepository
{
    Task<ExtractedKnowledge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtractedKnowledge>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExtractedKnowledge> AddAsync(ExtractedKnowledge extractedKnowledge, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExtractedKnowledge extractedKnowledge, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string?> GetRelevantKnowledgeAsync(Guid sessionId, string query, CancellationToken cancellationToken = default);
}