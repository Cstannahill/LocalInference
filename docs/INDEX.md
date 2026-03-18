# LocalInference Documentation

Welcome to the LocalInference API documentation. This guide will help you understand, deploy, and use the API.

## Quick Links

- [README](../README.md) - Project overview and quick start
- [Setup Guide](SETUP_GUIDE.md) - Installation and configuration
- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Examples](EXAMPLES.md) - Code examples and tutorials
- [Architecture](ARCHITECTURE.md) - Technical architecture details
- [Deployment](DEPLOYMENT.md) - Production deployment guide
- [Changelog](../CHANGELOG.md) - Version history

## What is LocalInference?

LocalInference is a high-performance, modular General Inference API compatible with OpenAI's API specification. It's designed for:

- **Local LLM Inference** - Run models locally with Ollama
- **Cloud Provider Support** - Use OpenRouter or other providers
- **Session Management** - Persistent conversation state
- **Context Optimization** - Sliding window token management
- **Technical RAG** - Semantic search for documentation

## Getting Started (5 minutes)

### 1. Prerequisites

- .NET 9.0 SDK: `dotnet --version`
- PostgreSQL 14+: `psql --version`
- Ollama: `ollama serve` and `ollama pull Qwen3.5-2B-UC`

### 2. Install

```bash
# Clone repository
git clone <repository-url>
cd LocalInference

# Restore dependencies
dotnet restore

# Configure database connection
# Edit src/LocalInference.Api/appsettings.json
# Default: Host=localhost;Database=LocalInference;Username=postgres;Password=postgres

# Run migrations
cd src/LocalInference.Api
dotnet ef database update

# Start API
dotnet run
```

The API will be available at `http://localhost:5000`

### 3. Test (Verified Working)

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

# Test chat completions
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [{"role": "user", "content": "Hello!"}],
    "stream": false
  }'
```

Expected response includes chat completion with `session_id` for follow-up messages.

## Documentation Structure

### For Users

| Document                          | Purpose                                 |
| --------------------------------- | --------------------------------------- |
| [README](../README.md)            | Project overview, features, quick start |
| [Setup Guide](SETUP_GUIDE.md)     | Detailed installation instructions      |
| [API Reference](API_REFERENCE.md) | Complete endpoint documentation         |
| [Examples](EXAMPLES.md)           | Code samples and tutorials              |

### For Developers

| Document                          | Purpose                    |
| --------------------------------- | -------------------------- |
| [Architecture](ARCHITECTURE.md)   | System design and patterns |
| [API Reference](API_REFERENCE.md) | Request/response schemas   |
| [Examples](EXAMPLES.md)           | Integration patterns       |

### For DevOps

| Document                        | Purpose                 |
| ------------------------------- | ----------------------- |
| [Deployment](DEPLOYMENT.md)     | Production deployment   |
| [Setup Guide](SETUP_GUIDE.md)   | Database setup          |
| [Architecture](ARCHITECTURE.md) | Infrastructure overview |

## Key Concepts

### Sessions

Sessions replace the traditional character/conversation model:

```csharp
// Create a session
POST /api/sessions
{
  "name": "My Chat",
  "contextWindowTokens": 8192
}

// Use in chat
POST /v1/chat/completions
{
  "session_id": "550e8400-e29b-41d4-a716-446655440000",
  "messages": [{"role": "user", "content": "Hello"}]
}
```

### Context Management

Automatic token budget management:

- **Sliding Window**: Keeps recent messages
- **Summarization**: Compresses old messages
- **Smart Compression**: Automatically optimizes

### Inference Configurations

Reusable configuration profiles:

```json
{
  "name": "Llama 3.2",
  "modelIdentifier": "llama3.2",
  "providerType": "Ollama",
  "temperature": 0.7,
  "systemPrompt": "You are a helpful assistant."
}
```

### Technical RAG

Semantic search for technical content:

```bash
# Add document
POST /api/retrieval/documents
{
  "title": "API Docs",
  "content": "...",
  "documentType": "TechnicalDocumentation"
}

# Query
POST /api/retrieval/query
{
  "query": "How to authenticate?",
  "maxResults": 5
}
```

## API Compatibility

### OpenAI-Compatible Endpoints

| Endpoint         | OpenAI | LocalInference |
| ---------------- | ------ | -------------- |
| Chat Completions | ✅     | ✅             |
| Streaming        | ✅     | ✅             |
| Models           | ✅     | Partial        |
| Embeddings       | ✅     | Via retrieval  |

### Request/Response Format

LocalInference uses OpenAI-compatible request/response formats:

```json
// Request
{
  "model": "llama3.2",
  "messages": [
    {"role": "system", "content": "You are helpful."},
    {"role": "user", "content": "Hello"}
  ],
  "temperature": 0.7,
  "stream": false
}

// Response
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1700000000,
  "model": "llama3.2",
  "choices": [...],
  "usage": {...}
}
```

## Common Tasks

### Create a Session

```bash
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"name": "My Session"}'
```

### Stream Response

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"messages": [{"role": "user", "content": "Hello"}], "stream": true}'
```

### Add Documentation

```bash
# Create document
curl -X POST http://localhost:5000/api/retrieval/documents \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Python Guide",
    "content": "Python is a programming language...",
    "documentType": "TechnicalDocumentation",
    "language": "python"
  }'

# Index for search
curl -X POST http://localhost:5000/api/retrieval/documents/{id}/index
```

### Get Session Statistics

```bash
curl http://localhost:5000/api/sessions/{id}/statistics
```

## Troubleshooting

### Database Connection Issues

```
Error: NpgsqlException: Connection refused

Solution:
1. Verify PostgreSQL is running
2. Check connection string in appsettings.json
3. Ensure database exists: CREATE DATABASE LocalInference;
```

### Ollama Connection Issues

```
Error: HttpRequestException: Connection refused

Solution:
1. Verify Ollama is running: curl http://localhost:11434/api/tags
2. Check base URL configuration
3. Ensure model is pulled: ollama pull llama3.2
```

### Token Budget Exceeded

```
Error: TokenBudgetExceededException

Solution:
1. Increase context window: {"context_window_tokens": 16384}
2. Enable compression (automatic)
3. Clear old messages: POST /api/sessions/{id}/clear
```

## Support

- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Documentation**: This documentation site

## Contributing

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

See [Architecture](ARCHITECTURE.md) for development guidelines.

## License

MIT License - See LICENSE file for details.

---

**Next Steps:**

- Read the [Setup Guide](SETUP_GUIDE.md) for detailed installation
- Explore [Examples](EXAMPLES.md) for code samples
- Review [API Reference](API_REFERENCE.md) for endpoint details
- Check [Deployment](DEPLOYMENT.md) for production setup
