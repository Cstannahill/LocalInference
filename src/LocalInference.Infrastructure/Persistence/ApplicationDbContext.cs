using LocalInference.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalInference.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<ContextMessage> ContextMessages => Set<ContextMessage>();
    public DbSet<InferenceConfig> InferenceConfigs => Set<InferenceConfig>();
    public DbSet<ContextCheckpoint> ContextCheckpoints => Set<ContextCheckpoint>();
    public DbSet<TechnicalDocument> TechnicalDocuments => Set<TechnicalDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<SystemProfile> SystemProfiles => Set<SystemProfile>();
    public DbSet<ReferenceData> ReferenceData => Set<ReferenceData>();
    public DbSet<ReferenceDataItem> ReferenceDataItems => Set<ReferenceDataItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
