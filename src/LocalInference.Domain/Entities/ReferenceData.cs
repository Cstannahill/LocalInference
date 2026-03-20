using LocalInference.Domain.Common;

namespace LocalInference.Domain.Entities;

public class ReferenceData : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<ReferenceDataItem> Items { get; set; } = new List<ReferenceDataItem>();
}