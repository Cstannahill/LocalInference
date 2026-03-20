using LocalInference.Domain.Entities;

namespace LocalInference.Application.Abstractions.Persistence;

/// <summary>
/// Repository for managing reference data (knowledge bases).
/// </summary>
public interface IReferenceDataRepository
{
    Task<ReferenceData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceData>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReferenceData> AddAsync(ReferenceData referenceData, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReferenceData referenceData, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string?> GetRelevantReferenceDataAsync(Guid sessionId, string query, CancellationToken cancellationToken = default);
}