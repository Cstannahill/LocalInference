using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using LocalInference.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalInference.Infrastructure.Persistence.Repositories;

public class TechnicalDocumentRepository : ITechnicalDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public TechnicalDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<TechnicalDocument?> GetByIdWithChunksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TechnicalDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TechnicalDocument>> GetByTypeAsync(DocumentType type, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalDocuments
            .AsNoTracking()
            .Where(d => d.DocumentType == type)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TechnicalDocument>> GetUnindexedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalDocuments
            .AsNoTracking()
            .Where(d => !d.IsIndexed)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TechnicalDocument document, CancellationToken cancellationToken = default)
    {
        await _context.TechnicalDocuments.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TechnicalDocument document, CancellationToken cancellationToken = default)
    {
        _context.TechnicalDocuments.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _context.TechnicalDocuments.FindAsync(new object[] { id }, cancellationToken);
        if (document != null)
        {
            _context.TechnicalDocuments.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
