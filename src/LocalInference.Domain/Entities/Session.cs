using LocalInference.Domain.Common;
using LocalInference.Domain.Enums;

namespace LocalInference.Domain.Entities;

public sealed class Session : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid InferenceConfigId { get; private set; }
    public InferenceConfig InferenceConfig { get; private set; } = null!;
    public int ContextWindowTokens { get; private set; }
    public int MaxOutputTokens { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    private readonly List<ContextMessage> _messages = new();
    public IReadOnlyCollection<ContextMessage> Messages => _messages.AsReadOnly();

    private readonly List<ContextCheckpoint> _checkpoints = new();
    public IReadOnlyCollection<ContextCheckpoint> Checkpoints => _checkpoints.AsReadOnly();

    private Session() { }

    public static Session Create(string name, InferenceConfig config, int contextWindowTokens = 8192, int maxOutputTokens = 2048)
    {
        return new Session
        {
            Name = name,
            InferenceConfigId = config.Id,
            ContextWindowTokens = contextWindowTokens,
            MaxOutputTokens = maxOutputTokens,
            IsActive = true
        };
    }

    public void AddMessage(MessageRole role, string content, int tokenCount)
    {
        var message = ContextMessage.Create(Id, role, content, tokenCount, _messages.Count);
        _messages.Add(message);
        LastActivityAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void AddCheckpoint(int startMessageIndex, int endMessageIndex, string summary, int compressedTokenCount)
    {
        var checkpoint = ContextCheckpoint.Create(Id, startMessageIndex, endMessageIndex, summary, compressedTokenCount);
        _checkpoints.Add(checkpoint);
        MarkUpdated();
    }

    public void UpdateName(string name)
    {
        Name = name;
        MarkUpdated();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        MarkUpdated();
    }

    public void UpdateInferenceConfig(Guid inferenceConfigId, InferenceConfig inferenceConfig)
    {
        InferenceConfigId = inferenceConfigId;
        InferenceConfig = inferenceConfig;
        MarkUpdated();
    }

    public void UpdateContextWindowTokens(int contextWindowTokens)
    {
        ContextWindowTokens = contextWindowTokens;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public IReadOnlyList<ContextMessage> GetMessagesForContext(int maxTokens)
    {
        var result = new List<ContextMessage>();
        int currentTokens = 0;

        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            var message = _messages[i];
            if (currentTokens + message.TokenCount > maxTokens)
                break;

            result.Insert(0, message);
            currentTokens += message.TokenCount;
        }

        return result;
    }

    public int GetTotalTokenCount()
    {
        return _messages.Sum(m => m.TokenCount);
    }
}
