using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalInference.Infrastructure.Persistence.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly ApplicationDbContext _context;

    public SessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Include(s => s.InferenceConfig)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Session?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .Include(s => s.InferenceConfig)
            .Include(s => s.Messages)
            .Include(s => s.Checkpoints)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Include(s => s.InferenceConfig)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Include(s => s.InferenceConfig)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        await _context.Sessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        // Update the session entity. The session is already tracked from GetByIdWithMessagesAsync,
        // so new messages added to its Messages collection will be automatically tracked as "Added"
        _context.Sessions.Update(session);
        
        // Explicitly ensure new messages and checkpoints are marked as "Added", not "Modified"
        foreach (var message in session.Messages)
        {
            var entry = _context.Entry(message);
            if (entry.State == EntityState.Detached)
            {
                _context.ContextMessages.Add(message);
            }
        }

        foreach (var checkpoint in session.Checkpoints)
        {
            var entry = _context.Entry(checkpoint);
            if (entry.State == EntityState.Detached)
            {
                _context.ContextCheckpoints.Add(checkpoint);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions.FindAsync(new object[] { id }, cancellationToken);
        if (session != null)
        {
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetTotalTokenCountAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ContextMessages
            .Where(m => m.SessionId == sessionId)
            .SumAsync(m => m.TokenCount, cancellationToken);
    }
}
