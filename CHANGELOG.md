# Changelog

All notable changes to LocalInference API.

## [1.0.1] - 2024-01-18

### Fixed

- **Critical:** DbUpdateConcurrencyException on chat completions endpoint
  - Root cause: Entity state confusion during session save after inference
  - Solution: Implemented fresh session reload pattern before persistence
  - Session.cs: Removed conflicting InferenceConfig assignment
  - InferenceService.cs: Fresh load via `GetByIdWithMessagesAsync()` before `SaveMessagesAsync()`
  - SessionRepository.cs: Proper entity state management in `UpdateAsync()`

- **DTO Mapping:** Null reference exception in `MapToDto()`
  - Now accepts optional InferenceConfig parameter
  - Safely handles sessions without explicit config assignment

- **Dependency Injection:** TechnicalSummarizationService registration
  - Added proper DI configuration in DependencyInjection extensions
  - Resolved missing provider factory bindings

### Improved

- Entity Framework Core query splitting for multi-collection loads
- Optimistic concurrency control implementation stability
- Documentation with tested curl commands and working examples
- Setup guide with comprehensive troubleshooting section
- API Reference with actual endpoint examples
- Architecture documentation of entity state management patterns

### Tested

- ✅ Full chat completions flow (POST /v1/chat/completions)
- ✅ Inference config creation and management
- ✅ Session persistence and message history
- ✅ Multi-turn conversations with session reuse
- ✅ Ollama integration at localhost:11434
- ✅ PostgreSQL entity tracking and concurrency control
- ✅ Database migrations and schema initialization

### Documentation Updates

- SETUP_GUIDE.md: Added verified test cases and entity state management troubleshooting
- EXAMPLES.md: Complete working curl examples with Qwen3.5-2B-UC model
- API_REFERENCE.md: Tested endpoints with real model identifiers
- ARCHITECTURE.md: Detailed entity state management strategy
- DEPLOYMENT.md: Production entity state management guidelines
- README.md: Updated quick start with verification checklist

## [1.0.0] - 2024-01-15

### Added

- Initial release of General Inference API
- OpenAI-compatible `/v1/chat/completions` endpoint
- Session-based conversation management
- Sliding window token management with intelligent compression
- Technical RAG system with semantic search
- Multi-provider support (Ollama, OpenRouter)
- PostgreSQL persistence with EF Core
- Streaming response support via Server-Sent Events
- Context checkpointing for long conversations
- Document chunking and embedding generation
- Token budget management and optimization
- Session statistics and monitoring
- Inference configuration management
- Health check endpoints
- Docker and Kubernetes deployment support

### Architecture

- Clean Architecture with Domain-Driven Design
- Repository pattern for data access
- Provider factory pattern for LLM backends
- Value objects for token budgets and context windows
- Domain events for audit trails

### API Endpoints

- `POST /v1/chat/completions` - OpenAI-compatible chat completions
- `POST /v1/inference` - Direct inference endpoint
- `GET /health` - Health check
- Session management endpoints (`/api/sessions`)
- Configuration endpoints (`/api/configs`)
- Retrieval endpoints (`/api/retrieval`)

### Security

- Non-root container execution
- Secrets management via environment variables
- HTTPS/TLS support
- Rate limiting ready
- CORS configuration support

### Documentation

- Comprehensive README
- API reference documentation
- Setup and deployment guides
- Architecture documentation
- Code examples in multiple languages

## Migration from LocalChat

### Breaking Changes

- Replaced Character/Conversation/Persona with Session model
- Replaced Lorebook with TechnicalDocument system
- New database schema (requires migration)
- Updated API endpoints (OpenAI-compatible)

### Migration Steps

1. Export existing data from LocalChat
2. Deploy new LocalInference infrastructure
3. Transform and import data
4. Update client applications to use new API

## Roadmap

### [1.1.0] - Planned

- Function calling support
- Multi-modal input (images)
- Fine-tuning endpoints
- Advanced RAG with hybrid search
- Redis caching layer
- WebSocket support for streaming

### [1.2.0] - Planned

- Agent framework integration
- Tool use capabilities
- Conversation branching
- Advanced analytics dashboard
- Prometheus metrics export

### [2.0.0] - Future

- Distributed inference
- Model quantization support
- Custom model hosting
- Enterprise SSO integration
- Advanced access control
