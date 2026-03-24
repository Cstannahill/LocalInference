# LocalInference API

A high-performance, modular General Inference API compatible with OpenAI's API specification. Built for local LLM inference with advanced context management, sliding window token optimization, and technical RAG capabilities.

## Features

- **OpenAI-Compatible API**: Drop-in replacement for OpenAI's chat completions API
- **Multi-Provider Support**: Ollama, OpenRouter, and extensible provider architecture
- **Advanced Context Management**: Sliding window token management with intelligent compression
- **Technical RAG**: Optimized retrieval for technical documentation and code
- **Session-Based Architecture**: Persistent conversation state with checkpointing
- **Streaming Support**: Real-time token streaming for all endpoints
- **PostgreSQL Backend**: Production-ready persistence with JSONB vector storage

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 14+
- Ollama (for local inference): https://ollama.ai

### Installation

```bash
# Clone the repository
git clone https://github.com/Cstannahill/LocalInference
cd LocalInference

# Restore dependencies
dotnet restore

# Configure database (edit appsettings.json with PostgreSQL connection)
# Default: Host=localhost;Database=LocalInference;Username=postgres;Password=postgres

# Apply migrations
cd src/LocalInference.Api
dotnet ef database update

# Start the API
dotnet run
```

### Verify Installation

```bash
# Health check
curl http://localhost:5000/health

# Create inference config
curl -X POST http://localhost:5000/api/inference-configs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Ollama Default",
    "modelIdentifier": "Qwen3.5-2B-UC",
    "providerType": "Ollama",
    "temperature": 0.7,
    "topP": 0.9,
    "contextWindow": 8192,
    "maxTokens": 2048,
    "isDefault": true
  }'

# Test chat completions (requires Ollama with Qwen3.5-2B-UC model)
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [{"role": "user", "content": "Hello!"}],
    "stream": false
  }'
```

### First Run Checklist

- ✅ .NET 9.0 SDK installed: `dotnet --version`
- ✅ PostgreSQL running: `psql --version`
- ✅ Ollama running: `curl http://localhost:11434/api/tags`
- ✅ Model available: `ollama pull Qwen3.5-2B-UC`
- ✅ Migrations applied: `cd src/LocalInference.Api && dotnet ef migrations list`
- ✅ API running: `dotnet run` (should show listening on port 5000)

## API Overview

### Base URL

```
http://localhost:5000
```

### Authentication

Currently, the API runs without authentication in local mode. For production deployment, add authentication middleware as needed.

## Core Concepts

### Sessions

Sessions replace the traditional character/persona model. A session:

- Maintains conversation history
- Has configurable context window size
- Supports multiple inference configurations
- Automatically manages token budgets

### Inference Configurations

Reusable configuration profiles defining:

- Model identifier and provider
- Generation parameters (temperature, top_p, etc.)
- System prompts
- Token limits

### Context Management

Intelligent token management strategies:

- **Sliding Window**: Keeps most recent messages within budget
- **Summarization**: Compresses older messages into checkpoints
- **Smart Compression**: Automatically applies optimal strategy

### Technical RAG

Retrieval-augmented generation optimized for:

- Technical documentation
- Code references
- API documentation
- Structured knowledge bases

## API Endpoints

### Chat Completions (OpenAI Compatible)

#### Create Chat Completion

```http
POST /v1/chat/completions
```

**Request Body:**

```json
{
  "model": "Qwen3.5-2B-UC",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "Hello!" }
  ],
  "temperature": 0.7,
  "max_tokens": 2048,
  "stream": false
}
```

**Response:**

```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1700000000,
  "model": "llama3.2",
  "session_id": "550e8400-e29b-41d4-a716-446655440000",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 25,
    "completion_tokens": 9,
    "total_tokens": 34
  }
}
```

#### Streaming

Set `stream: true` for Server-Sent Events (SSE) streaming:

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.2",
    "messages": [{"role": "user", "content": "Hello"}],
    "stream": true
  }'
```

### Session Management

#### Create Session

```http
POST /api/sessions
```

```json
{
  "name": "My Chat Session",
  "description": "Technical discussion about APIs",
  "inferenceConfigId": "550e8400-e29b-41d4-a716-446655440000",
  "contextWindowTokens": 8192,
  "maxOutputTokens": 2048
}
```

#### List Sessions

```http
GET /api/sessions?activeOnly=true&skip=0&take=100
```

#### Get Session

```http
GET /api/sessions/{id}
```

#### Update Session

```http
PUT /api/sessions/{id}
```

```json
{
  "name": "Updated Session Name",
  "contextWindowTokens": 16384
}
```

#### Delete Session

```http
DELETE /api/sessions/{id}
```

#### Get Session Statistics

```http
GET /api/sessions/{id}/statistics
```

**Response:**

```json
{
  "totalMessages": 42,
  "totalTokens": 15360,
  "averageMessageLength": 245,
  "checkpointCount": 3,
  "compressionRatio": 0.32,
  "firstMessageAt": "2024-01-15T10:30:00Z",
  "lastMessageAt": "2024-01-15T14:45:00Z"
}
```

### Inference Configuration

#### Create Configuration

```http
POST /api/configs
```

```json
{
  "name": "Llama 3.2 Default",
  "modelIdentifier": "llama3.2",
  "providerType": "Ollama",
  "temperature": 0.7,
  "topP": 0.9,
  "maxTokens": 2048,
  "systemPrompt": "You are a helpful coding assistant.",
  "isDefault": true
}
```

#### List Configurations

```http
GET /api/configs
```

#### Update Configuration

```http
PUT /api/configs/{id}
```

```json
{
  "temperature": 0.5,
  "maxTokens": 4096
}
```

### Retrieval (RAG)

#### Query Documents

```http
POST /api/retrieval/query
```

```json
{
  "query": "How do I configure dependency injection?",
  "maxResults": 5,
  "maxTokens": 2000,
  "minScore": 0.7,
  "documentTypes": ["TechnicalDocumentation", "CodeReference"],
  "language": "csharp"
}
```

**Response:**

```json
{
  "query": "How do I configure dependency injection?",
  "results": [
    {
      "content": "To configure DI in ASP.NET Core, use services.AddSingleton<T>()...",
      "source": "ASP.NET Core Documentation",
      "score": 0.92,
      "tokenCount": 156,
      "documentType": "TechnicalDocumentation",
      "language": "csharp"
    }
  ]
}
```

#### Add Document

```http
POST /api/retrieval/documents
```

```json
{
  "title": "API Documentation",
  "content": "Full document content here...",
  "documentType": "TechnicalDocumentation",
  "language": "csharp",
  "framework": "ASP.NET Core"
}
```

#### Index Document

```http
POST /api/retrieval/documents/{id}/index
```

#### Reindex All Documents

```http
POST /api/retrieval/reindex
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=LocalInference;Username=postgres;Password=postgres"
  },
  "Inference": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434"
    },
    "OpenRouter": {
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### Environment Variables

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=LocalInference;..."
export Inference__Ollama__BaseUrl="http://localhost:11434"
export Inference__OpenRouter__ApiKey="your-api-key"
```

## Architecture

### Domain Layer

- **Entities**: Session, InferenceConfig, ContextMessage, ContextCheckpoint, TechnicalDocument
- **Value Objects**: TokenBudget, ContextWindowState, RetrievalResult
- **Enums**: MessageRole, InferenceProviderType, DocumentType

### Application Layer

- **Services**: InferenceService, SessionManagementService, ContextManager
- **Abstractions**: IInferenceProvider, IEmbeddingProvider, ITechnicalRetrievalService

### Infrastructure Layer

- **Persistence**: PostgreSQL with EF Core
- **Inference**: Ollama and OpenRouter providers
- **Retrieval**: Vector similarity search with cosine distance
- **Summarization**: Technical context compression

### API Layer

- Minimal API endpoints
- OpenAI-compatible request/response models
- Streaming support via SSE

## Advanced Usage

### Custom Context Management

```csharp
// Compress context using specific strategy
await contextManager.CompressContextAsync(
    sessionId,
    CompressionStrategy.SmartCompression);

// Get current context state
var state = await contextManager.GetContextStateAsync(sessionId);
Console.WriteLine($"Utilization: {state.UtilizationRatio:P}");
```

### Direct Inference

```http
POST /v1/inference
```

```json
{
  "configId": "550e8400-e29b-41d4-a716-446655440000",
  "messages": [{ "role": "user", "content": "Explain quantum computing" }],
  "temperature": 0.3,
  "maxTokens": 1024
}
```

### Session with Retrieval Context

```json
{
  "model": "llama3.2",
  "messages": [{ "role": "user", "content": "What does this API do?" }],
  "session_id": "existing-session-id",
  "retrieval_context": [
    {
      "content": "The API provides LLM inference...",
      "source": "Documentation",
      "relevance_score": 0.95
    }
  ]
}
```

## Performance Optimization

### Token Budget Management

- Default context window: 8192 tokens
- Reserve for output: 2048 tokens
- Reserve for system: 512 tokens
- Available for context: ~5600 tokens

### Database Indexes

- Sessions: IsActive, LastActivityAt, CreatedAt
- ContextMessages: SessionId + SequenceNumber, IsSummarized
- TechnicalDocuments: DocumentType, IsIndexed, Language
- DocumentChunks: TechnicalDocumentId, ChunkIndex

### Caching Strategies

- Inference configurations cached in memory
- Session state optimized for frequent reads
- Document embeddings stored with JSONB

## Troubleshooting

### Common Issues

**Database Connection Failed**

```
Check PostgreSQL is running and connection string is correct.
Ensure database exists: CREATE DATABASE LocalInference;
```

**Ollama Connection Failed**

```
Verify Ollama is running: curl http://localhost:11434/api/tags
Check base URL in configuration
```

**Token Budget Exceeded**

```
Reduce context window size or enable compression
Check session statistics for utilization ratio
```

### Logs

```bash
# Enable debug logging
export Logging__LogLevel__Default=Debug

# View logs
dotnet run --verbosity diagnostic
```

## Development

### Project Structure

```
src/
├── LocalInference.Domain/          # Domain entities
├── LocalInference.Application/     # Business logic
├── LocalInference.Infrastructure/  # Data access, providers
└── LocalInference.Api/             # HTTP API
```

### Adding a New Provider

1. Implement `IInferenceProvider` interface
2. Register in `InferenceProviderFactory`
3. Add configuration options
4. Create HTTP client registration

### Running Tests

```bash
dotnet test
```

## License

MIT License - See LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## Support

- Issues: GitHub Issues
- Discussions: GitHub Discussions
- Documentation: This README and API docs
