using LocalInference.Domain.Common;

namespace LocalInference.Domain.Entities;

public sealed class ContextCheckpoint : AuditableEntity
{
    public Guid SessionId { get; private set; }
    public Session Session { get; private set; } = null!;
    public int StartMessageIndex { get; private set; }
    public int EndMessageIndex { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public int OriginalTokenCount { get; private set; }
    public int CompressedTokenCount { get; private set; }
    public double CompressionRatio => OriginalTokenCount > 0 ? (double)CompressedTokenCount / OriginalTokenCount : 0;
    public bool IsActive { get; private set; }
    public Guid? ReplacedByCheckpointId { get; private set; }

    private ContextCheckpoint() { }

    public static ContextCheckpoint Create(
        Guid sessionId,
        int startMessageIndex,
        int endMessageIndex,
        string summary,
        int compressedTokenCount)
    {
        var originalTokenCount = (endMessageIndex - startMessageIndex + 1) * 50;

        return new ContextCheckpoint
        {
            SessionId = sessionId,
            StartMessageIndex = startMessageIndex,
            EndMessageIndex = endMessageIndex,
            Summary = summary,
            OriginalTokenCount = originalTokenCount,
            CompressedTokenCount = compressedTokenCount,
            IsActive = true
        };
    }

    public void MarkAsReplaced(Guid replacementCheckpointId)
    {
        ReplacedByCheckpointId = replacementCheckpointId;
        IsActive = false;
        MarkUpdated();
    }

    public void UpdateSummary(string summary, int compressedTokenCount)
    {
        Summary = summary;
        CompressedTokenCount = compressedTokenCount;
        MarkUpdated();
    }
}
