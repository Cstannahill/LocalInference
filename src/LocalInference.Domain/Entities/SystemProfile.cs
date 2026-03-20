using LocalInference.Domain.Common;

namespace LocalInference.Domain.Entities;

public class SystemProfile : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public int MaxContextTokens { get; set; } = 8192;
    public string DefaultModel { get; set; } = "llama3";
    public ICollection<ReferenceData> LinkedReferenceSets { get; set; } = new List<ReferenceData>();
}