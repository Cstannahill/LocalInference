# Architecture

Technical architecture documentation for LocalInference API.

## Table of Contents

1. [Overview](#overview)
2. [Domain Layer](#domain-layer)
3. [Application Layer](#application-layer)
4. [Infrastructure Layer](#infrastructure-layer)
5. [API Layer](#api-layer)
6. [Data Flow](#data-flow)
7. [Context Management](#context-management)
8. [Retrieval System](#retrieval-system)
9. [Provider Architecture](#provider-architecture)

## Overview

LocalInference follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────┐
│           API Layer                 │
│    (Controllers/Endpoints)          │
├─────────────────────────────────────┤
│        Application Layer            │
│    (Services, Use Cases)            │
├─────────────────────────────────────┤
│       Infrastructure Layer          │
│  (Persistence, External Services)   │
├─────────────────────────────────────┤
│         Domain Layer                │
│    (Entities, Value Objects)        │
└─────────────────────────────────────┘
```

## Domain Layer

### Core Entities

#### Session

The central entity replacing Character/Conversation/Persona:

```csharp
public class Session : AuditableEntity
{
    public string Name { get; private set; }
    public Guid InferenceConfigId { get; private set; }
    public int ContextWindowTokens { get; private set; }
    public int MaxOutputTokens { get; private set; }
    public bool IsActive { get; private set; }

    private List<ContextMessage> _messages;
    private List<ContextCheckpoint> _checkpoints;
}
```

**Responsibilities:**

- Maintains conversation history
- Manages token budget
- Tracks activity timestamps
- Aggregates checkpoints

#### ContextMessage

Individual messages with metadata:

```csharp
public class ContextMessage : AuditableEntity
{
    public Guid SessionId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public int TokenCount { get; private set; }
    public int SequenceNumber { get; private set; }
    public bool IsSummarized { get; private set; }
}
```

**Key Features:**

- Token count tracking for budget management
- Sequence numbers for ordering
- Summarization state tracking
- Soft deletion via IsSummarized flag

#### InferenceConfig

Reusable configuration profile:

```csharp
public class InferenceConfig : AuditableEntity
{
    public string Name { get; private set; }
    public string ModelIdentifier { get; private set; }
    public InferenceProviderType ProviderType { get; private set; }
    public double Temperature { get; private set; }
    public double TopP { get; private set; }
    public string SystemPrompt { get; private set; }
    // ... additional parameters
}
```

#### ContextCheckpoint

Summarized conversation segments:

```csharp
public class ContextCheckpoint : AuditableEntity
{
    public Guid SessionId { get; private set; }
    public int StartMessageIndex { get; private set; }
    public int EndMessageIndex { get; private set; }
    public string Summary { get; private set; }
    public int OriginalTokenCount { get; private set; }
    public int CompressedTokenCount { get; private set; }
    public bool IsActive { get; private set; }
}
```

**Purpose:**

- Compress old messages to save tokens
- Maintain conversation context
- Track compression ratios

#### TechnicalDocument

RAG document storage:

```csharp
public class TechnicalDocument : AuditableEntity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string Language { get; private set; }
    public string Framework { get; private set; }
    public bool IsIndexed { get; private set; }

    private List<DocumentChunk> _chunks;
}
```

### Value Objects

#### TokenBudget

Immutable token allocation:

```csharp
public record TokenBudget
{
    public int TotalBudget { get; }
    public int ReservedForOutput { get; }
    public int ReservedForSystem { get; }
    public int AvailableForContext => TotalBudget - ReservedForOutput - ReservedForSystem;
}
```

#### ContextWindowState

Current context utilization:

```csharp
public record ContextWindowState
{
    public int TotalTokens { get; init; }
    public int SystemTokens { get; init; }
    public int ContextTokens { get; init; }
    public int AvailableTokens { get; init; }
    public double UtilizationRatio { get; init; }
    public bool IsWithinBudget { get; init; }
}
```

## Application Layer

### Core Services

#### InferenceService

Orchestrates LLM inference:

```csharp
public interface IInferenceService
{
    Task<InferenceResult> GenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<InferenceStreamResult> StreamGenerateAsync(
        Guid sessionId,
        string userMessage,
        InferenceOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**Responsibilities:**

- Build message context
- Call inference providers
- Save messages to session
- Handle streaming

#### SessionManagementService

CRUD operations for sessions:

```csharp
public interface ISessionManagementService
{
    Task<SessionDto> CreateSessionAsync(CreateSessionRequest request);
    Task<SessionDto?> GetSessionAsync(Guid sessionId);
    Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(ListSessionsRequest request);
    Task UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request);
    Task DeleteSessionAsync(Guid sessionId);
    Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId);
}
```

#### ContextManager

Token budget and context optimization:

```csharp
public interface IContextManager
{
    Task<IReadOnlyList<ContextMessageDto>> GetOptimizedContextAsync(
        Guid sessionId,
        string currentMessage,
        CancellationToken cancellationToken = default);

    Task<ContextWindowState> GetContextStateAsync(Guid sessionId);
    Task CompressContextAsync(Guid sessionId, CompressionStrategy strategy);
    Task TrimContextAsync(Guid sessionId, int targetTokenCount);
}
```

**Strategies:**

- `SummarizeOldest`: Create checkpoints from old messages
- `RemoveOldest`: Mark old messages as summarized
- `SlidingWindow`: Keep only recent messages
- `SmartCompression`: Automatically choose best strategy

### Abstractions

#### IInferenceProvider

Unified interface for LLM providers:

```csharp
public interface IInferenceProvider
{
    string ProviderName { get; }

    Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<InferenceStreamChunk> StreamCompletionAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default);

    Task<int> EstimateTokenCountAsync(string text, string modelIdentifier);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
```

#### IEmbeddingProvider

Text embedding generation:

```csharp
public interface IEmbeddingProvider
{
    string ProviderName { get; }
    int EmbeddingDimensions { get; }

    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts);
}
```

#### ITechnicalRetrievalService

Semantic document retrieval:

```csharp
public interface ITechnicalRetrievalService
{
    Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken = default);

    Task IndexDocumentAsync(Guid documentId);
    Task ReindexAllAsync();
}
```

## Infrastructure Layer

### Persistence

#### PostgreSQL Schema

**Sessions Table:**

```sql
CREATE TABLE "Sessions" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(256) NOT NULL,
    "Description" varchar(1024),
    "InferenceConfigId" uuid NOT NULL REFERENCES "InferenceConfigs",
    "ContextWindowTokens" integer DEFAULT 8192,
    "MaxOutputTokens" integer DEFAULT 2048,
    "IsActive" boolean DEFAULT true,
    "LastActivityAt" timestamp,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL
);

CREATE INDEX "IX_Sessions_IsActive" ON "Sessions" ("IsActive");
CREATE INDEX "IX_Sessions_LastActivityAt" ON "Sessions" ("LastActivityAt");
```

**ContextMessages Table:**

```sql
CREATE TABLE "ContextMessages" (
    "Id" uuid PRIMARY KEY,
    "SessionId" uuid NOT NULL REFERENCES "Sessions" ON DELETE CASCADE,
    "Role" text NOT NULL,
    "Content" text NOT NULL,
    "TokenCount" integer NOT NULL,
    "SequenceNumber" integer NOT NULL,
    "IsSummarized" boolean DEFAULT false,
    "CheckpointId" uuid,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL
);

CREATE INDEX "IX_ContextMessages_SessionId_SequenceNumber"
    ON "ContextMessages" ("SessionId", "SequenceNumber");
```

**DocumentChunks Table:**

```sql
CREATE TABLE "DocumentChunks" (
    "Id" uuid PRIMARY KEY,
    "TechnicalDocumentId" uuid NOT NULL REFERENCES "TechnicalDocuments" ON DELETE CASCADE,
    "Content" text NOT NULL,
    "EmbeddingJson" jsonb,
    "TokenCount" integer NOT NULL,
    "ChunkIndex" integer NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL
);
```

### Inference Providers

#### Ollama Provider

```csharp
public class OllamaInferenceProvider : IInferenceProvider
{
    private readonly HttpClient _httpClient;

    public async Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken)
    {
        var ollamaRequest = new OllamaChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = MapMessages(request.Messages),
            Stream = false,
            Options = MapOptions(request)
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/chat", ollamaRequest, cancellationToken);

        return MapResponse(response);
    }
}
```

**Features:**

- Native Ollama API integration
- Streaming via SSE
- Token count estimation
- Health checks

#### OpenRouter Provider

```csharp
public class OpenRouterInferenceProvider : IInferenceProvider
{
    private readonly HttpClient _httpClient;

    public async Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken)
    {
        var openRouterRequest = new OpenRouterChatRequest
        {
            Model = request.ModelIdentifier,
            Messages = MapMessages(request.Messages),
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/chat/completions", openRouterRequest, cancellationToken);

        return MapResponse(response);
    }
}
```

### Retrieval System

#### TechnicalRetrievalService

```csharp
public class TechnicalRetrievalService : ITechnicalRetrievalService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ITechnicalDocumentRepository _documentRepository;

    public async Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
        string query,
        RetrievalOptions options,
        CancellationToken cancellationToken)
    {
        // Generate query embedding
        var queryEmbedding = await _embeddingProvider
            .GenerateEmbeddingAsync(query, cancellationToken);

        // Search documents
        var documents = await _documentRepository
            .GetAllAsync(cancellationToken);

        // Calculate similarities
        var results = documents
            .SelectMany(d => d.Chunks)
            .Select(c => new
            {
                Chunk = c,
                Score = CosineSimilarity(queryEmbedding, c.Embedding)
            })
            .Where(x => x.Score >= options.MinScore)
            .OrderByDescending(x => x.Score)
            .Take(options.MaxResults)
            .Select(x => RetrievalResult.Create(
                x.Chunk.Content,
                x.Chunk.TechnicalDocument.Title,
                x.Score,
                x.Chunk.TokenCount))
            .ToList();

        return results;
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var normA = Math.Sqrt(a.Sum(x => x * x));
        var normB = Math.Sum(b.Sum(y => y * y));
        return dotProduct / (normA * normB);
    }
}
```

**Chunking Strategy:**

- Default chunk size: 512 tokens
- Overlap: 50 tokens
- Preserves sentence boundaries
- Metadata tracking (position, index)

## API Layer

### Endpoint Organization

```csharp
public static class ChatCompletionsEndpoints
{
    public static IEndpointRouteBuilder MapChatCompletionsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1").WithTags("Chat Completions");

        group.MapPost("/chat/completions", async (
            [FromBody] ChatCompletionRequest request,
            IInferenceService inferenceService) =>
        {
            // Handle streaming vs non-streaming
            if (request.Stream)
            {
                return Results.Stream(async (stream, ct) => {
                    // Stream implementation
                }, "text/event-stream");
            }

            // Non-streaming response
            var result = await inferenceService.GenerateAsync(...);
            return Results.Ok(MapToResponse(result));
        });

        return app;
    }
}
```

### Request/Response Models

OpenAI-compatible models ensure drop-in compatibility:

```csharp
public class ChatCompletionRequest
{
    public string? Model { get; set; }
    public required List<ChatMessageRequest> Messages { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; }
    public string? SessionId { get; set; }
}

public class ChatCompletionResponse
{
    public required string Id { get; set; }
    public required string Object { get; set; }
    public long Created { get; set; }
    public required string Model { get; set; }
    public required string SessionId { get; set; }
    public required ChatCompletionChoice[] Choices { get; set; }
    public required UsageInfo Usage { get; set; }
}
```

## Data Flow

### Chat Completion Flow (Complete Request-Response Cycle)

```
Client Request: POST /v1/chat/completions
  ↓
ChatCompletionsEndpoints.MapChatCompletionsEndpoints()
  ├─ Validate request (model, messages, parameters)
  ├─ Load or create Session (SessionManagementService)
  │   └─ If session_id provided: Get from SessionRepository
  │   └─ Else: Create new Session with InferenceConfig
  ↓
InferenceService.GenerateAsync(sessionId, userMessage, options)
  ├─ Load default InferenceConfig if not specified
  ├─ Validate context window and token budget
  ├─ Build message history from ContextMessages
  ├─ Invoke OllamaInferenceProvider.CompleteAsync()
  │   └─ HTTP POST to Ollama at localhost:11434/api/chat
  │   └─ Receive completion token response
  ↓
SaveMessagesAsync(session, userMessage, assistantMessage)
  ├─ CRITICAL: Reload Session fresh from database
  │   └─ SELECT * FROM Sessions WHERE Id = @id
  │   └─ Include ContextMessages and ContextCheckpoints
  ├─ Create user ContextMessage entity
  ├─ Create assistant ContextMessage entity
  ├─ Add both messages to session.Messages collection
  ├─ Call SessionRepository.UpdateAsync(session)
  │   └─ SaveChangesAsync() persists:
  │       ├─ UPDATE ContextMessages batch
  │       └─ UPDATE Sessions
  └─ Return completion response
  ↓
Response: 200 OK with chat completion including session_id
```

### Entity State Management Strategy

**Problem:** Optimistic concurrency exceptions occur when EF can't find expected rows during UPDATE.

**Solution:** Fresh session reload pattern ensures entity state matches database reality.

```csharp
// PATTERN: Why we reload the session
var session = await repository.GetByIdWithMessagesAsync(sessionId);

// ... do inference work ...
var completionResponse = await inferenceService.GenerateAsync(...);

// CRITICAL: Reload fresh to avoid entity state confusion
session = await repository.GetByIdWithMessagesAsync(sessionId);

// Now safely add messages to fresh tracked entity
session.Messages.Add(userMessage);
session.Messages.Add(assistantMessage);

// SaveChangesAsync() knows the correct entity state
await repository.UpdateAsync(session);
```

**Why this works:**

1. Session is loaded as tracked entity (optina concurrency checks enabled)
2. After inference, reload gets fresh state from database
3. Any stale references from before reload are replaced
4. EF correctly tracks new Message entities as "Added"
5. SaveChangesAsync() generates valid UPDATE/INSERT statements

**Key Files:**

- `InferenceService.cs` line 234: `SaveMessagesAsync()` implementation
- `SessionRepository.cs` line 89: `UpdateAsync()` with SaveChangesAsync()
- `GetByIdWithMessagesAsync()`: Loads session with full related entity graph

### Context Compression Flow

```
1. Token budget check
   |
   v
2. If utilization > 90%
   |-- SummarizeOldest strategy
   |   |-- Select oldest N messages
   |   |-- Call TechnicalSummarizationService
   |   |-- Create ContextCheckpoint
   |   |-- Mark messages as summarized
   |
   v
3. If utilization > 75%
   |-- SlidingWindow strategy
   |   |-- Calculate available tokens
   |   |-- Mark oldest messages outside window
   |
   v
4. Update Session
```

### Document Indexing Flow

```
1. Client POST /api/retrieval/documents
   |
   v
2. Create TechnicalDocument entity
   |
   v
3. Chunk content
   |-- Split into ~512 token chunks
   |-- Add overlap
   |
   v
4. Generate embeddings
   |-- Call IEmbeddingProvider
   |-- Store in DocumentChunk.EmbeddingJson
   |
   v
5. Save to database
   |
   v
6. Mark as indexed
```

## Context Management

### Token Budget Allocation

```
Total Context Window: 8192 tokens
├── Reserved for Output: 2048 tokens
├── Reserved for System: 512 tokens
└── Available for Context: 5632 tokens
    ├── Checkpoints (max 40%): ~2250 tokens
    └── Recent Messages: ~3380 tokens
```

### Compression Strategies

#### SummarizeOldest

```csharp
private async Task SummarizeOldestMessagesAsync(Session session)
{
    var messagesToSummarize = session.Messages
        .Where(m => !m.IsSummarized)
        .OrderBy(m => m.SequenceNumber)
        .Take(10)
        .ToList();

    var summary = await _summarizationService
        .SummarizeConversationAsync(messageSummaries, options);

    session.AddCheckpoint(
        startMessageIndex: messagesToSummarize.First().SequenceNumber,
        endMessageIndex: messagesToSummarize.Last().SequenceNumber,
        summary: summary,
        compressedTokenCount: EstimateTokens(summary));

    foreach (var message in messagesToSummarize)
    {
        message.MarkAsSummarized(session.Checkpoints.Last().Id);
    }
}
```

#### SlidingWindow

```csharp
private async Task ApplySlidingWindowAsync(Session session)
{
    var availableTokens = session.ContextWindowTokens
        - ReserveForOutput
        - ReserveForSystem;

    var messages = session.Messages
        .OrderBy(m => m.SequenceNumber)
        .ToList();

    var currentTokens = 0;
    var firstIncludedIndex = messages.Count;

    // Walk backwards from newest
    for (int i = messages.Count - 1; i >= 0; i--)
    {
        if (currentTokens + messages[i].TokenCount <= availableTokens)
        {
            currentTokens += messages[i].TokenCount;
            firstIncludedIndex = i;
        }
        else
        {
            break;
        }
    }

    // Mark messages outside window as summarized
    for (int i = 0; i < firstIncludedIndex; i++)
    {
        if (!messages[i].IsSummarized)
        {
            messages[i].MarkAsSummarized(Guid.Empty);
        }
    }
}
```

## Retrieval System

### Vector Search

```csharp
public async Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
    string query,
    RetrievalOptions options)
{
    // 1. Embed query
    var queryEmbedding = await _embeddingProvider
        .GenerateEmbeddingAsync(query);

    // 2. Load candidate documents
    var documents = await _documentRepository
        .GetByTypeAsync(options.DocumentType);

    // 3. Calculate cosine similarity
    var scoredChunks = documents
        .SelectMany(d => d.Chunks)
        .Select(c => new
        {
            Chunk = c,
            Score = CosineSimilarity(queryEmbedding, c.Embedding)
        })
        .Where(x => x.Score >= options.MinScore)
        .OrderByDescending(x => x.Score);

    // 4. Apply token budget
    var results = new List<RetrievalResult>();
    var totalTokens = 0;

    foreach (var item in scoredChunks)
    {
        if (totalTokens + item.Chunk.TokenCount <= options.MaxTokens)
        {
            results.Add(MapToResult(item));
            totalTokens += item.Chunk.TokenCount;
        }
    }

    return results;
}
```

### Chunking Algorithm

```csharp
private List<TextChunk> ChunkDocument(string content, int chunkSize, int overlap)
{
    var chunks = new List<TextChunk>();
    var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var step = chunkSize - overlap;

    for (int i = 0; i < words.Length; i += step)
    {
        var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
        if (chunkWords.Length == 0) break;

        // Preserve sentence boundaries
        var chunkText = string.Join(' ', chunkWords);
        var lastPeriod = chunkText.LastIndexOf('.');
        if (lastPeriod > chunkText.Length * 0.8)
        {
            chunkText = chunkText[..(lastPeriod + 1)];
        }

        chunks.Add(new TextChunk(
            chunkText,
            startPosition: i,
            endPosition: i + chunkWords.Length));
    }

    return chunks;
}
```

## Provider Architecture

### Factory Pattern

```csharp
public class InferenceProviderFactory : IInferenceProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<InferenceProviderType, Type> _providers;

    public InferenceProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providers = new()
        {
            [InferenceProviderType.Ollama] = typeof(OllamaInferenceProvider),
            [InferenceProviderType.OpenRouter] = typeof(OpenRouterInferenceProvider)
        };
    }

    public IInferenceProvider GetProvider(InferenceProviderType type)
    {
        var providerType = _providers[type];
        return (IInferenceProvider)_serviceProvider
            .GetRequiredService(providerType);
    }

    public IInferenceProvider GetProvider(string modelIdentifier)
    {
        // Route based on model identifier format
        if (modelIdentifier.Contains('/'))
            return GetProvider(InferenceProviderType.OpenRouter);

        return GetProvider(InferenceProviderType.Ollama);
    }
}
```

### Adding a New Provider

1. **Implement Interface:**

```csharp
public class CustomProvider : IInferenceProvider
{
    public string ProviderName => "Custom";

    public async Task<InferenceResponse> CompleteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

2. **Register Services:**

```csharp
services.AddHttpClient<CustomProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.custom.com");
});
```

3. **Add to Factory:**

```csharp
_providers[InferenceProviderType.Custom] = typeof(CustomProvider);
```

## Performance Considerations

### Database Optimization

- **Indexes:** All foreign keys, query filters
- **JSONB:** Embeddings stored as JSONB for flexibility
- **Batching:** Chunk operations in batches of 100
- **Lazy Loading:** Use `AsNoTracking()` for read-only queries

### Memory Management

- **Streaming:** Process streams without buffering entire response
- **Pooling:** Reuse HttpClient instances
- **Disposal:** Proper disposal of database contexts

### Caching Strategy

```csharp
// Inference configs rarely change
services.AddSingleton<InferenceConfigCache>();

// Session state cached for active sessions
services.AddMemoryCache();
```

## Security Considerations

### Input Validation

- Model binding validates request shapes
- FluentValidation for complex rules
- SQL injection prevention via EF Core parameterized queries

### Output Encoding

- JSON serialization handles escaping
- No raw HTML output

### Secrets Management

```csharp
// Use environment variables or secret managers
// Never commit API keys to source control
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddAzureKeyVault(...);
```
