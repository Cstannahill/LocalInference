using LocalInference.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace LocalInference.Application.Abstractions.Persistence
{
    /// <summary>
    /// Repository for SystemProfile entities.
    /// </summary>
    public interface ISystemProfileRepository
    {
        Task<SystemProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SystemProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}