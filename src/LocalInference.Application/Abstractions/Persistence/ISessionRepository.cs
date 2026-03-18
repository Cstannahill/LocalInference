using LocalInference.Domain.Entities;

namespace LocalInference.Application.Abstractions.Persistence;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Session?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
    Task UpdateAsync(Session session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetTotalTokenCountAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
