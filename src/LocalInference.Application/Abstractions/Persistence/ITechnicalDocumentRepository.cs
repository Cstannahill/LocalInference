using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;

namespace LocalInference.Application.Abstractions.Persistence;

public interface ITechnicalDocumentRepository
{
    Task<TechnicalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TechnicalDocument?> GetByIdWithChunksAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicalDocument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicalDocument>> GetByTypeAsync(DocumentType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnicalDocument>> GetUnindexedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TechnicalDocument document, CancellationToken cancellationToken = default);
    Task UpdateAsync(TechnicalDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
