using LocalInference.Application.Abstractions.Inference;
using LocalInference.Application.Abstractions.Persistence;
using LocalInference.Application.Abstractions.Retrieval;
using LocalInference.Domain.Entities;
using LocalInference.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LocalInference.Infrastructure.Retrieval;

public class TechnicalRetrievalService : ITechnicalRetrievalService
{
    private readonly ITechnicalDocumentRepository _documentRepository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILogger<TechnicalRetrievalService> _logger;

    private const int DEFAULT_CHUNK_SIZE = 512;
    private const int CHUNK_OVERLAP = 50;

    public TechnicalRetrievalService(
        ITechnicalDocumentRepository documentRepository,
        IEmbeddingProvider embeddingProvider,
        ILogger<TechnicalRetrievalService> logger)
    {
        _documentRepository = documentRepository;
        _embeddingProvider = embeddingProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(query, cancellationToken);
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        var results = new List<RetrievalResult>();

        foreach (var document in documents.Where(d => d.IsIndexed))
        {
            if (options.DocumentTypes?.Count > 0 &&
                !options.DocumentTypes.Contains(document.DocumentType.ToString()))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(options.Language) &&
                document.Language != options.Language)
            {
                continue;
            }

            var documentWithChunks = await _documentRepository.GetByIdWithChunksAsync(document.Id, cancellationToken);
            if (documentWithChunks?.Chunks == null) continue;

            foreach (var chunk in documentWithChunks.Chunks)
            {
                if (chunk.Embedding == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                if (similarity >= options.MinScore)
                {
                    results.Add(RetrievalResult.Create(
                        chunk.Content,
                        document.Title,
                        similarity,
                        chunk.TokenCount,
                        document.DocumentType.ToString(),
                        document.Language,
                        chunk.ChunkIndex));
                }
            }
        }

        var filteredResults = results
            .OrderByDescending(r => r.Score)
            .Take(options.MaxResults * 2)
            .ToList();

        var finalResults = new List<RetrievalResult>();
        var totalTokens = 0;

        foreach (var result in filteredResults)
        {
            if (totalTokens + result.TokenCount <= options.MaxTokens)
            {
                finalResults.Add(result);
                totalTokens += result.TokenCount;
            }
        }

        return finalResults;
    }

    public async Task<IReadOnlyList<RetrievalResult>> RetrieveForSessionAsync(
        Guid sessionId,
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken = default)
    {
        return await RetrieveAsync(query, options, cancellationToken);
    }

    public async Task IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdWithChunksAsync(documentId, cancellationToken);
        if (document == null) return;

        document.ClearChunks();

        var chunks = ChunkDocument(document.Content, DEFAULT_CHUNK_SIZE, CHUNK_OVERLAP);
        var chunkTexts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingProvider.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = DocumentChunk.Create(
                documentId,
                chunks[i].Content,
                chunks[i].StartPosition,
                chunks[i].EndPosition,
                i);

            chunk.SetEmbedding(embeddings[i]);
            document.AddChunk(chunk);
        }

        document.MarkAsIndexed(_embeddingProvider.ProviderName);
        await _documentRepository.UpdateAsync(document, cancellationToken);
    }

    public async Task RemoveDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null) return;

        await _documentRepository.DeleteAsync(documentId, cancellationToken);
    }

    public async Task ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        foreach (var document in documents)
        {
            await IndexDocumentAsync(document.Id, cancellationToken);
        }
    }

    private double CalculateCosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length");

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private List<TextChunk> ChunkDocument(string content, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var step = chunkSize - overlap;

        for (int i = 0; i < words.Length; i += step)
        {
            var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
            if (chunkWords.Length == 0) break;

            var chunkContent = string.Join(' ', chunkWords);
            var startPos = i;
            var endPos = Math.Min(i + chunkWords.Length, words.Length);

            chunks.Add(new TextChunk(chunkContent, startPos, endPos));
        }

        return chunks;
    }

    private record TextChunk(string Content, int StartPosition, int EndPosition);
}
