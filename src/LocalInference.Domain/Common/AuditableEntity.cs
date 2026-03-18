namespace LocalInference.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    protected AuditableEntity() : base()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected AuditableEntity(Guid id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
