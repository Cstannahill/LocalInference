# API Reference

Complete reference for all LocalInference API endpoints.

## Base URL

```
http://localhost:5000
```

## Authentication

The API currently runs without authentication for local development. For production, implement authentication middleware.

## Content Types

All endpoints accept and return `application/json` unless otherwise specified.

Streaming endpoints return `text/event-stream`.

---

## Chat Completions

### Create Chat Completion

Creates a completion for the chat messages.

**Endpoint:** `POST /v1/chat/completions`

**Request Body:**

| Field       | Type    | Required | Description                              |
| ----------- | ------- | -------- | ---------------------------------------- |
| model       | string  | No       | Model identifier (overrides config)      |
| messages    | array   | Yes      | Array of message objects                 |
| temperature | float   | No       | Sampling temperature (0-2, default: 0.7) |
| top_p       | float   | No       | Nucleus sampling (0-1, default: 0.9)     |
| max_tokens  | integer | No       | Maximum tokens to generate               |
| stream      | boolean | No       | Stream response (default: false)         |
| session_id  | string  | No       | Existing session ID                      |
| config_id   | string  | No       | Inference configuration ID               |

**Message Object:**

| Field   | Type   | Required | Description                      |
| ------- | ------ | -------- | -------------------------------- |
| role    | string | Yes      | "system", "user", or "assistant" |
| content | string | Yes      | Message content                  |
| name    | string | No       | Name for the participant         |

**Response (Non-Streaming):**

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
        "content": "Response text here..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 25,
    "completion_tokens": 15,
    "total_tokens": 40
  }
}
```

**Response (Streaming):**

Server-Sent Events format:

```
data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1700000000,"model":"llama3.2","choices":[{"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1700000000,"model":"llama3.2","choices":[{"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1700000000,"model":"llama3.2","choices":[{"delta":{"content":"!"},"finish_reason":null}]}

data: {"id":"chatcmpl-abc123","object":"chat.completion.chunk","created":1700000000,"model":"llama3.2","choices":[{"delta":{},"finish_reason":"stop"}]}

data: [DONE]
```

**Example:**

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [
      {"role": "user", "content": "What is machine learning?"}
    ],
    "temperature": 0.7,
    "max_tokens": 500,
    "stream": false
  }'
```

**Response (200 OK):**

```json
{
  "id": "chatcmpl-abc123def456",
  "object": "chat.completion",
  "created": 1705324800,
  "model": "Qwen3.5-2B-UC",
  "session_id": "550e8400-e29b-41d4-a716-446655440000",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Machine learning is a subset of artificial intelligence that enables systems to learn and improve from experience without explicit programming..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 47,
    "total_tokens": 62
  }
}
```

**Using Existing Session:**

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "session_id": "550e8400-e29b-41d4-a716-446655440000",
    "model": "Qwen3.5-2B-UC",
    "messages": [
      {"role": "user", "content": "Tell me more about neural networks."}
    ],
    "temperature": 0.5,
    "stream": false
  }'
```

---

## Inference

### Direct Inference

Perform inference directly with a configuration ID.

**Endpoint:** `POST /v1/inference`

**Request Body:**

| Field       | Type          | Required | Description                |
| ----------- | ------------- | -------- | -------------------------- |
| config_id   | string (UUID) | Yes      | Inference configuration ID |
| messages    | array         | Yes      | Array of message objects   |
| temperature | float         | No       | Sampling temperature       |
| top_p       | float         | No       | Nucleus sampling           |
| max_tokens  | integer       | No       | Maximum tokens to generate |

**Response:**

```json
{
  "content": "Generated response text...",
  "prompt_tokens": 25,
  "completion_tokens": 15,
  "total_tokens": 40,
  "model": "llama3.2",
  "finish_reason": "stop"
}
```

---

## Sessions

### Create Session

Create a new inference session.

**Endpoint:** `POST /api/sessions`

**Request Body:**

| Field                 | Type          | Required | Description                              |
| --------------------- | ------------- | -------- | ---------------------------------------- |
| name                  | string        | Yes      | Session name                             |
| description           | string        | No       | Session description                      |
| inference_config_id   | string (UUID) | No       | Config ID (uses default if not provided) |
| context_window_tokens | integer       | No       | Context window size (default: 8192)      |
| max_output_tokens     | integer       | No       | Max output tokens (default: 2048)        |

**Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "My Session",
  "description": "Technical discussion",
  "inference_config_id": "660f9500-f30c-52e5-b827-557766551111",
  "inference_config_name": "Llama 3.2 Default",
  "context_window_tokens": 8192,
  "max_output_tokens": 2048,
  "is_active": true,
  "created_at": "2024-01-15T10:30:00Z",
  "updated_at": "2024-01-15T10:30:00Z",
  "last_activity_at": null,
  "message_count": 0,
  "total_token_count": 0
}
```

**Status:** 201 Created

---

### List Sessions

List all sessions with optional filtering.

**Endpoint:** `GET /api/sessions`

**Query Parameters:**

| Parameter   | Type    | Default | Description                    |
| ----------- | ------- | ------- | ------------------------------ |
| active_only | boolean | false   | Filter to active sessions only |
| skip        | integer | 0       | Number of sessions to skip     |
| take        | integer | 100     | Number of sessions to return   |

**Response:**

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "My Session",
    "inference_config_name": "Llama 3.2 Default",
    "is_active": true,
    "created_at": "2024-01-15T10:30:00Z",
    "last_activity_at": "2024-01-15T14:45:00Z",
    "message_count": 42
  }
]
```

---

### Get Session

Get detailed information about a specific session.

**Endpoint:** `GET /api/sessions/{id}`

**Path Parameters:**

| Parameter | Type          | Description |
| --------- | ------------- | ----------- |
| id        | string (UUID) | Session ID  |

**Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "My Session",
  "description": "Technical discussion",
  "inference_config_id": "660f9500-f30c-52e5-b827-557766551111",
  "inference_config_name": "Llama 3.2 Default",
  "context_window_tokens": 8192,
  "max_output_tokens": 2048,
  "is_active": true,
  "created_at": "2024-01-15T10:30:00Z",
  "updated_at": "2024-01-15T14:45:00Z",
  "last_activity_at": "2024-01-15T14:45:00Z",
  "message_count": 42,
  "total_token_count": 15360
}
```

**Status:** 200 OK, 404 Not Found

---

### Update Session

Update session properties.

**Endpoint:** `PUT /api/sessions/{id}`

**Path Parameters:**

| Parameter | Type          | Description |
| --------- | ------------- | ----------- |
| id        | string (UUID) | Session ID  |

**Request Body:**

| Field                 | Type          | Required | Description             |
| --------------------- | ------------- | -------- | ----------------------- |
| name                  | string        | No       | New session name        |
| description           | string        | No       | New description         |
| inference_config_id   | string (UUID) | No       | New config ID           |
| context_window_tokens | integer       | No       | New context window size |

**Status:** 204 No Content, 404 Not Found

---

### Delete Session

Delete a session and all associated data.

**Endpoint:** `DELETE /api/sessions/{id}`

**Path Parameters:**

| Parameter | Type          | Description |
| --------- | ------------- | ----------- |
| id        | string (UUID) | Session ID  |

**Status:** 204 No Content

---

### Get Session Statistics

Get detailed statistics about a session.

**Endpoint:** `GET /api/sessions/{id}/statistics`

**Response:**

```json
{
  "total_messages": 42,
  "total_tokens": 15360,
  "average_message_length": 245,
  "checkpoint_count": 3,
  "compression_ratio": 0.32,
  "first_message_at": "2024-01-15T10:30:00Z",
  "last_message_at": "2024-01-15T14:45:00Z"
}
```

---

### Clear Session Context

Mark a session as inactive (soft delete).

**Endpoint:** `POST /api/sessions/{id}/clear`

**Status:** 204 No Content

---

## Inference Configurations

### Create Configuration

Create a new inference configuration.

**Endpoint:** `POST /api/inference-configs`

**Request Body:**

| Field           | Type    | Required | Description                                 |
| --------------- | ------- | -------- | ------------------------------------------- |
| name            | string  | Yes      | Configuration name (e.g., "Ollama Default") |
| modelIdentifier | string  | Yes      | Model ID (e.g., "Qwen3.5-2B-UC")            |
| providerType    | string  | Yes      | "Ollama", "OpenRouter", "OpenAi", "Custom"  |
| temperature     | float   | No       | Sampling temperature (0-2, default: 0.7)    |
| topP            | float   | No       | Nucleus sampling (0-1, default: 0.9)        |
| topK            | integer | No       | Top-k sampling (default: 40)                |
| contextWindow   | integer | No       | Model context window size (default: 8192)   |
| maxTokens       | integer | No       | Max generation tokens (default: 2048)       |
| systemPrompt    | string  | No       | Default system prompt                       |
| isDefault       | boolean | No       | Set as default configuration                |

**Example Request:**

```bash
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
```

**Response:**

```json
{
  "id": "660f9500-f30c-52e5-b827-557766551111",
  "name": "Ollama Default",
  "modelIdentifier": "Qwen3.5-2B-UC",
  "providerType": "Ollama",
  "temperature": 0.7,
  "topP": 0.9,
  "contextWindow": 8192,
  "maxTokens": 2048,
  "isDefault": true,
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

**Status:** 201 Created

---

### List Configurations

Get all inference configurations.

**Endpoint:** `GET /api/inference-configs`

**Example:**

```bash
curl http://localhost:5000/api/inference-configs
```

**Response:**

```json
[
  {
    "id": "660f9500-f30c-52e5-b827-557766551111",
    "name": "Ollama Default",
    "modelIdentifier": "Qwen3.5-2B-UC",
    "providerType": "Ollama",
    "temperature": 0.7,
    "topP": 0.9,
    "contextWindow": 8192,
    "maxTokens": 2048,
    "isDefault": true,
    "createdAt": "2024-01-15T10:00:00Z",
    "updatedAt": "2024-01-15T10:00:00Z"
  }
]
```

---

### Get Configuration

Get a specific configuration.

**Endpoint:** `GET /api/inference-configs/{id}`

**Example:**

```bash
curl http://localhost:5000/api/inference-configs/660f9500-f30c-52e5-b827-557766551111
```

**Response:** Full configuration object (see Create Configuration)

**Status:** 200 OK, 404 Not Found

---

### Update Configuration

Update an existing configuration.

**Endpoint:** `PUT /api/inference-configs/{id}`

**Request Body:** Same fields as Create Configuration (all optional for updates)

**Status:** 200 OK, 404 Not Found

---

### Delete Configuration

Delete a configuration.

**Endpoint:** `DELETE /api/inference-configs/{id}`

**Status:** 204 No Content, 404 Not Found

---

## Retrieval (RAG)

### Query Documents

Search technical documents using semantic similarity.

**Endpoint:** `POST /api/retrieval/query`

**Request Body:**

| Field             | Type    | Required | Description                                 |
| ----------------- | ------- | -------- | ------------------------------------------- |
| query             | string  | Yes      | Search query                                |
| max_results       | integer | No       | Max results (default: 5)                    |
| max_tokens        | integer | No       | Max total tokens (default: 2000)            |
| min_score         | float   | No       | Minimum similarity score (default: 0.7)     |
| document_types    | array   | No       | Filter by document types                    |
| language          | string  | No       | Filter by programming language              |
| prioritize_recent | boolean | No       | Prioritize recent documents (default: true) |

**Response:**

```json
{
  "query": "How to configure dependency injection?",
  "results": [
    {
      "content": "To configure DI in ASP.NET Core...",
      "source": "ASP.NET Core Documentation",
      "score": 0.92,
      "token_count": 156,
      "document_type": "TechnicalDocumentation",
      "language": "csharp",
      "chunk_index": 3
    }
  ]
}
```

---

### List Documents

Get all technical documents.

**Endpoint:** `GET /api/retrieval/documents`

**Response:**

```json
[
  {
    "id": "770fa600-g41d-63f6-c938-668877662222",
    "title": "API Documentation",
    "content": "Preview of content...",
    "document_type": "TechnicalDocumentation",
    "source_url": null,
    "source_path": null,
    "language": "csharp",
    "framework": "ASP.NET Core",
    "version": null,
    "token_count": 5000,
    "is_indexed": true,
    "last_indexed_at": "2024-01-15T12:00:00Z",
    "created_at": "2024-01-15T11:00:00Z",
    "updated_at": "2024-01-15T12:00:00Z"
  }
]
```

---

### Create Document

Add a new technical document.

**Endpoint:** `POST /api/retrieval/documents`

**Request Body:**

| Field         | Type   | Required | Description                                                                                          |
| ------------- | ------ | -------- | ---------------------------------------------------------------------------------------------------- |
| title         | string | Yes      | Document title                                                                                       |
| content       | string | Yes      | Full document content                                                                                |
| document_type | string | Yes      | "TechnicalDocumentation", "CodeReference", "GeneralKnowledge", "ConversationContext", "SystemPrompt" |
| source_url    | string | No       | Source URL                                                                                           |
| source_path   | string | No       | File path                                                                                            |
| language      | string | No       | Programming language                                                                                 |
| framework     | string | No       | Framework name                                                                                       |
| version       | string | No       | Version string                                                                                       |

**Status:** 201 Created

---

### Index Document

Generate embeddings for a document.

**Endpoint:** `POST /api/retrieval/documents/{id}/index`

**Status:** 204 No Content

---

### Reindex All Documents

Regenerate embeddings for all documents.

**Endpoint:** `POST /api/retrieval/reindex`

**Status:** 204 No Content

---

## Health

### Health Check

Check API health status.

**Endpoint:** `GET /health` or `GET /v1/health`

**Response:**

```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T15:30:00Z"
}
```

---

## Error Responses

All errors follow this format:

```json
{
  "error": "Error description",
  "code": "ERROR_CODE",
  "details": {
    "field": "Additional details"
  }
}
```

**Common Status Codes:**

| Code | Meaning               |
| ---- | --------------------- |
| 200  | Success               |
| 201  | Created               |
| 204  | No Content            |
| 400  | Bad Request           |
| 404  | Not Found             |
| 500  | Internal Server Error |

**Common Error Codes:**

| Code                     | Description                       |
| ------------------------ | --------------------------------- |
| SESSION_NOT_FOUND        | Requested session does not exist  |
| CONFIG_NOT_FOUND         | Inference configuration not found |
| TOKEN_BUDGET_EXCEEDED    | Context window exceeded           |
| INFERENCE_PROVIDER_ERROR | LLM provider error                |
| DOCUMENT_NOT_FOUND       | Technical document not found      |
