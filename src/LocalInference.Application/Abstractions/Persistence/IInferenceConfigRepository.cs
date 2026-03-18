using LocalInference.Domain.Entities;

namespace LocalInference.Application.Abstractions.Persistence;

public interface IInferenceConfigRepository
{
    Task<InferenceConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InferenceConfig?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InferenceConfig>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InferenceConfig config, CancellationToken cancellationToken = default);
    Task UpdateAsync(InferenceConfig config, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearDefaultAsync(CancellationToken cancellationToken = default);
}
