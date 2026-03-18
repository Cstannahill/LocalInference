using LocalInference.Domain.Common;
using LocalInference.Domain.Enums;

namespace LocalInference.Domain.Entities;

public sealed class TechnicalDocument : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? SourcePath { get; private set; }
    public string? Language { get; private set; }
    public string? Framework { get; private set; }
    public string? Version { get; private set; }
    public int TokenCount { get; private set; }
    public bool IsIndexed { get; private set; }
    public DateTime? LastIndexedAt { get; private set; }
    public string? EmbeddingModel { get; private set; }

    private readonly List<DocumentChunk> _chunks = new();
    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

    private TechnicalDocument() { }

    public static TechnicalDocument Create(
        string title,
        string content,
        DocumentType documentType,
        string? sourceUrl = null,
        string? sourcePath = null,
        string? language = null,
        string? framework = null,
        string? version = null)
    {
        return new TechnicalDocument
        {
            Title = title,
            Content = content,
            DocumentType = documentType,
            SourceUrl = sourceUrl,
            SourcePath = sourcePath,
            Language = language,
            Framework = framework,
            Version = version,
            TokenCount = EstimateTokenCount(content)
        };
    }

    public void UpdateContent(string content)
    {
        Content = content;
        TokenCount = EstimateTokenCount(content);
        IsIndexed = false;
        MarkUpdated();
    }

    public void UpdateMetadata(
        string? title = null,
        string? sourceUrl = null,
        string? sourcePath = null,
        string? language = null,
        string? framework = null,
        string? version = null)
    {
        if (title != null) Title = title;
        if (sourceUrl != null) SourceUrl = sourceUrl;
        if (sourcePath != null) SourcePath = sourcePath;
        if (language != null) Language = language;
        if (framework != null) Framework = framework;
        if (version != null) Version = version;
        MarkUpdated();
    }

    public void MarkAsIndexed(string embeddingModel)
    {
        IsIndexed = true;
        LastIndexedAt = DateTime.UtcNow;
        EmbeddingModel = embeddingModel;
        MarkUpdated();
    }

    public void ClearChunks()
    {
        _chunks.Clear();
        MarkUpdated();
    }

    public void AddChunk(DocumentChunk chunk)
    {
        _chunks.Add(chunk);
        MarkUpdated();
    }

    private static int EstimateTokenCount(string text)
    {
        return (int)(text.Length / 4.0);
    }
}
