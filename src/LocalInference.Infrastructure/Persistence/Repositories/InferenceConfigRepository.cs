using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalInference.Infrastructure.Persistence.Repositories;

public class InferenceConfigRepository : IInferenceConfigRepository
{
    private readonly ApplicationDbContext _context;

    public InferenceConfigRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InferenceConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InferenceConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<InferenceConfig?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InferenceConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsDefault, cancellationToken);
    }

    public async Task<IReadOnlyList<InferenceConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InferenceConfigs
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InferenceConfig config, CancellationToken cancellationToken = default)
    {
        if (config.IsDefault)
        {
            await ClearDefaultAsync(cancellationToken);
        }

        await _context.InferenceConfigs.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InferenceConfig config, CancellationToken cancellationToken = default)
    {
        if (config.IsDefault)
        {
            await ClearDefaultAsync(cancellationToken);
        }

        _context.InferenceConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _context.InferenceConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (config != null)
        {
            _context.InferenceConfigs.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearDefaultAsync(CancellationToken cancellationToken = default)
    {
        await _context.InferenceConfigs
            .Where(c => c.IsDefault)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.IsDefault, false), cancellationToken);
    }
}
