using LocalInference.Domain.Common;
using LocalInference.Domain.Enums;

namespace LocalInference.Domain.Entities;

public sealed class ContextMessage : AuditableEntity
{
    public Guid SessionId { get; private set; }
    public Session Session { get; private set; } = null!;
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public int SequenceNumber { get; private set; }
    public bool IsSummarized { get; private set; }
    public Guid? CheckpointId { get; private set; }
    public ContextCheckpoint? Checkpoint { get; private set; }

    private ContextMessage() { }

    public static ContextMessage Create(Guid sessionId, MessageRole role, string content, int tokenCount, int sequenceNumber)
    {
        return new ContextMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            TokenCount = tokenCount,
            SequenceNumber = sequenceNumber
        };
    }

    public void MarkAsSummarized(Guid checkpointId)
    {
        IsSummarized = true;
        CheckpointId = checkpointId;
        MarkUpdated();
    }

    public void UpdateContent(string content, int tokenCount)
    {
        Content = content;
        TokenCount = tokenCount;
        MarkUpdated();
    }
}
