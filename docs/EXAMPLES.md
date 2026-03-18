# Examples

Practical examples for using the LocalInference API.

## Table of Contents

1. [Basic Chat](#basic-chat)
2. [Session Management](#session-management)
3. [Streaming Responses](#streaming-responses)
4. [Context Management](#context-management)
5. [Retrieval-Augmented Generation](#retrieval-augmented-generation)
6. [Multi-Provider Setup](#multi-provider-setup)
7. [Client Libraries](#client-libraries)

## Basic Chat

### Simple Chat Completion (Tested Working)

**Prerequisites:**

- Ollama running: `ollama serve` (default port 11434)
- Model pulled: `ollama pull Qwen3.5-2B-UC`

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [
      {"role": "user", "content": "Hello! What is 2 + 2?"}
    ],
    "temperature": 0.7,
    "max_tokens": 500,
    "stream": false
  }'
```

**Response:**

```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1700000000,
  "model": "Qwen3.5-2B-UC",
  "session_id": "550e8400-e29b-41d4-a716-446655440000",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "2 + 2 = 4"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 12,
    "completion_tokens": 5,
    "total_tokens": 17
  }
}
```

### Multi-Turn Conversation (Using Session)

**Step 1: Create a session**

```bash
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Math Tutor Session",
    "description": "Learning basic math",
    "contextWindowTokens": 8192,
    "maxOutputTokens": 2048
  }'
```

Response includes `id` - save this for the next request.

**Step 2: Use the session for follow-up messages**

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "session_id": "550e8400-e29b-41d4-a716-446655440000",
    "model": "Qwen3.5-2B-UC",
    "messages": [
      {
        "role": "system",
        "content": "You are a mathematics tutor. Explain concepts clearly."
      },
      {
        "role": "user",
        "content": "What is the quadratic formula?"
      }
    ],
    "temperature": 0.5,
    "stream": false
  }'
```

### With System Prompt

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [
      {
        "role": "system",
        "content": "You are an expert Python developer. Provide clean, documented code."
      },
      {
        "role": "user",
        "content": "Write a function to reverse a string."
      }
    ],
    "temperature": 0.7,
    "max_tokens": 500
  }'
```

### Using Python

```python
import requests

def chat_completion(messages, model="llama3.2"):
    response = requests.post(
        "http://localhost:5000/v1/chat/completions",
        json={
            "model": model,
            "messages": messages,
            "temperature": 0.7
        }
    )
    return response.json()

messages = [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "What is machine learning?"}
]

result = chat_completion(messages)
print(result["choices"][0]["message"]["content"])
```

### Using JavaScript

```javascript
async function chatCompletion(messages) {
  const response = await fetch("http://localhost:5000/v1/chat/completions", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      model: "llama3.2",
      messages: messages,
      temperature: 0.7,
    }),
  });
  return await response.json();
}

const messages = [
  { role: "user", content: "Explain quantum computing simply." },
];

chatCompletion(messages).then((result) => {
  console.log(result.choices[0].message.content);
});
```

## Session Management

### Create a Persistent Session

```bash
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Documentation Assistant Session",
    "description": "Analyzing technical documentation",
    "contextWindowTokens": 16384,
    "maxOutputTokens": 2048,
    "isActive": true
  }'
```

Response:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Documentation Assistant Session",
  "description": "Analyzing technical documentation",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastActivityAt": "2024-01-15T10:30:00Z"
}
```

### Store and Reuse Session Between Requests

**Linux/macOS:**

```bash
# Create session and save ID
SESSION_ID=$(curl -s -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"name": "CLI Session"}' | jq -r '.id')

# Use in first request
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{
    \"session_id\": \"$SESSION_ID\",
    \"model\": \"Qwen3.5-2B-UC\",
    \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}]
  }"

# Use same session in follow-up request
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{
    \"session_id\": \"$SESSION_ID\",
    \"model\": \"Qwen3.5-2B-UC\",
    \"messages\": [{\"role\": \"user\", \"content\": \"Remember who I am?\"}]
  }"
```

### Get Session Details

```bash
curl -X GET http://localhost:5000/api/sessions/550e8400-e29b-41d4-a716-446655440000
```

### Create Inference Config and Use It

```bash
# Create a custom config
CONFIG_ID=$(curl -s -X POST http://localhost:5000/api/inference-configs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Creative Writing",
    "modelIdentifier": "Qwen3.5-2B-UC",
    "providerType": "Ollama",
    "temperature": 0.9,
    "topP": 0.95,
    "contextWindow": 8192,
    "maxTokens": 1024,
    "systemPrompt": "You are a creative fiction writer.",
    "isDefault": false
  }' | jq -r '.id')

# Use the config in a chat request
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{
    \"config_id\": \"$CONFIG_ID\",
    \"messages\": [{\"role\": \"user\", \"content\": \"Write a short story about a robot\"}]
  }"
```

session_id = session_response.json()["id"]
print(f"Session created: {session_id}")

# Use session for multiple messages

messages = [
{"role": "user", "content": "What are Python decorators?"}
]

response = requests.post(
"http://localhost:5000/v1/chat/completions",
json={
"session_id": session_id,
"messages": messages
}
)

# Continue conversation - context is preserved

follow_up = requests.post(
"http://localhost:5000/v1/chat/completions",
json={
"session_id": session_id,
"messages": [{"role": "user", "content": "Can you show me an example?"}]
}
)

````

### Session Statistics

```python
# Get session stats
stats = requests.get(f"http://localhost:5000/api/sessions/{session_id}/statistics")
print(f"Total messages: {stats.json()['total_messages']}")
print(f"Total tokens: {stats.json()['total_tokens']}")
print(f"Compression ratio: {stats.json()['compression_ratio']}")
````

### List and Filter Sessions

```python
# List all active sessions
active_sessions = requests.get(
    "http://localhost:5000/api/sessions",
    params={"activeOnly": "true", "take": 10}
)

for session in active_sessions.json():
    print(f"{session['name']}: {session['message_count']} messages")
```

## Streaming Responses

### Stream with cURL

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.2",
    "messages": [{"role": "user", "content": "Write a story about AI."}],
    "stream": true
  }'
```

### Stream with Python

```python
import requests
import json

def stream_chat(messages):
    response = requests.post(
        "http://localhost:5000/v1/chat/completions",
        json={
            "model": "llama3.2",
            "messages": messages,
            "stream": True
        },
        stream=True
    )

    for line in response.iter_lines():
        if line:
            line = line.decode('utf-8')
            if line.startswith('data: '):
                data = line[6:]
                if data == '[DONE]':
                    break
                try:
                    chunk = json.loads(data)
                    content = chunk['choices'][0]['delta'].get('content', '')
                    if content:
                        print(content, end='', flush=True)
                except:
                    pass

messages = [{"role": "user", "content": "Count from 1 to 10 slowly."}]
stream_chat(messages)
```

### Stream with JavaScript

```javascript
async function streamChat(messages) {
  const response = await fetch("http://localhost:5000/v1/chat/completions", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      model: "llama3.2",
      messages: messages,
      stream: true,
    }),
  });

  const reader = response.body.getReader();
  const decoder = new TextDecoder();

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    const chunk = decoder.decode(value);
    const lines = chunk.split("\n");

    for (const line of lines) {
      if (line.startsWith("data: ")) {
        const data = line.slice(6);
        if (data === "[DONE]") return;

        try {
          const parsed = JSON.parse(data);
          const content = parsed.choices[0].delta.content;
          if (content) process.stdout.write(content);
        } catch {}
      }
    }
  }
}
```

## Context Management

### Understanding Token Budget

```python
import requests

session_id = "your-session-id"

# Get current context state
state = requests.get(f"http://localhost:5000/api/sessions/{session_id}")
session = state.json()

print(f"Context window: {session['context_window_tokens']} tokens")
print(f"Current usage: {session['total_token_count']} tokens")
print(f"Utilization: {session['total_token_count'] / session['context_window_tokens']:.1%}")
```

### Large Context Handling

```python
# For large documents, create a session with expanded context
large_session = requests.post(
    "http://localhost:5000/api/sessions",
    json={
        "name": "Document Analysis",
        "contextWindowTokens": 32768,  # 32k context
        "maxOutputTokens": 4096
    }
)

# The API automatically manages token budgets
# Old messages are summarized when approaching limits
```

## Retrieval Augmented Generation

### Add Technical Documentation

```python
import requests

# Add a code reference document
doc = requests.post(
    "http://localhost:5000/api/retrieval/documents",
    json={
        "title": "Python Async/Await Guide",
        "content": """
        Async/await in Python allows writing concurrent code.

        Key concepts:
        - async def defines a coroutine
        - await suspends execution until result is ready
        - asyncio.run() executes the main coroutine

        Example:
        async def fetch_data():
            await asyncio.sleep(1)
            return "data"
        """,
        "documentType": "CodeReference",
        "language": "python",
        "framework": "asyncio"
    }
)

doc_id = doc.json()["id"]

# Index the document for retrieval
requests.post(f"http://localhost:5000/api/retrieval/documents/{doc_id}/index")
```

### Query Documents

```python
# Search for relevant context
results = requests.post(
    "http://localhost:5000/api/retrieval/query",
    json={
        "query": "How do I use async await in Python?",
        "maxResults": 3,
        "minScore": 0.7,
        "language": "python"
    }
)

for result in results.json()["results"]:
    print(f"Score: {result['score']:.2f}")
    print(f"Source: {result['source']}")
    print(f"Content: {result['content'][:200]}...")
    print()
```

### RAG-Enhanced Chat

```python
# First, retrieve relevant context
query = "Explain Python decorators with examples"
retrieval = requests.post(
    "http://localhost:5000/api/retrieval/query",
    json={
        "query": query,
        "maxResults": 2,
        "maxTokens": 1000
    }
)

# Build context from retrieval results
context_parts = []
for result in retrieval.json()["results"]:
    context_parts.append(f"[{result['source']}]: {result['content']}")

context = "\n\n".join(context_parts)

# Use context in chat
messages = [
    {"role": "system", "content": f"Use this context to answer:\n\n{context}"},
    {"role": "user", "content": query}
]

response = requests.post(
    "http://localhost:5000/v1/chat/completions",
    json={"messages": messages}
)
```

## Multi-Provider Setup

### Configure Multiple Providers

```python
# Create Ollama config
ollama_config = requests.post(
    "http://localhost:5000/api/configs",
    json={
        "name": "Local Llama 3.2",
        "modelIdentifier": "llama3.2",
        "providerType": "Ollama",
        "temperature": 0.7,
        "isDefault": True
    }
)

# Create OpenRouter config for cloud models
openrouter_config = requests.post(
    "http://localhost:5000/api/configs",
    json={
        "name": "Claude 3.5 Sonnet",
        "modelIdentifier": "anthropic/claude-3.5-sonnet",
        "providerType": "OpenRouter",
        "temperature": 0.5
    }
)

# Use specific config
cloud_session = requests.post(
    "http://localhost:5000/api/sessions",
    json={
        "name": "Cloud AI Session",
        "inferenceConfigId": openrouter_config.json()["id"]
    }
)
```

### Direct Inference with Specific Config

```python
# Use a specific config without creating a session
response = requests.post(
    "http://localhost:5000/v1/inference",
    json={
        "configId": "config-uuid-here",
        "messages": [
            {"role": "user", "content": "Complex reasoning task..."}
        ],
        "temperature": 0.3
    }
)
```

## Client Libraries

### Python Client Class

```python
import requests
from typing import List, Dict, Optional, Iterator

class LocalInferenceClient:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url

    def chat(self, messages: List[Dict], model: str = "llama3.2", **kwargs) -> str:
        response = requests.post(
            f"{self.base_url}/v1/chat/completions",
            json={
                "model": model,
                "messages": messages,
                **kwargs
            }
        )
        return response.json()["choices"][0]["message"]["content"]

    def stream_chat(self, messages: List[Dict], **kwargs) -> Iterator[str]:
        response = requests.post(
            f"{self.base_url}/v1/chat/completions",
            json={"messages": messages, "stream": True, **kwargs},
            stream=True
        )

        for line in response.iter_lines():
            if line and line.startswith(b'data: '):
                data = line[6:].decode('utf-8')
                if data == '[DONE]':
                    break
                try:
                    import json
                    chunk = json.loads(data)
                    content = chunk['choices'][0]['delta'].get('content', '')
                    if content:
                        yield content
                except:
                    pass

    def create_session(self, name: str, **kwargs) -> str:
        response = requests.post(
            f"{self.base_url}/api/sessions",
            json={"name": name, **kwargs}
        )
        return response.json()["id"]

    def query_documents(self, query: str, **kwargs) -> List[Dict]:
        response = requests.post(
            f"{self.base_url}/api/retrieval/query",
            json={"query": query, **kwargs}
        )
        return response.json()["results"]

# Usage
client = LocalInferenceClient()

# Simple chat
response = client.chat([
    {"role": "user", "content": "Hello!"}
])
print(response)

# Streaming
for chunk in client.stream_chat([
    {"role": "user", "content": "Tell me a story."}
]):
    print(chunk, end='')

# With session
session_id = client.create_session("My Session")
# Use session_id in chat calls
```

### JavaScript/TypeScript Client

```typescript
interface Message {
  role: "system" | "user" | "assistant";
  content: string;
}

class LocalInferenceClient {
  private baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5000") {
    this.baseUrl = baseUrl;
  }

  async chat(messages: Message[], model: string = "llama3.2"): Promise<string> {
    const response = await fetch(`${this.baseUrl}/v1/chat/completions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ model, messages }),
    });
    const data = await response.json();
    return data.choices[0].message.content;
  }

  async *streamChat(messages: Message[]): AsyncGenerator<string> {
    const response = await fetch(`${this.baseUrl}/v1/chat/completions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ messages, stream: true }),
    });

    const reader = response.body!.getReader();
    const decoder = new TextDecoder();

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      const chunk = decoder.decode(value);
      const lines = chunk.split("\n");

      for (const line of lines) {
        if (line.startsWith("data: ")) {
          const data = line.slice(6);
          if (data === "[DONE]") return;
          try {
            const parsed = JSON.parse(data);
            const content = parsed.choices[0].delta.content;
            if (content) yield content;
          } catch {}
        }
      }
    }
  }

  async createSession(name: string): Promise<string> {
    const response = await fetch(`${this.baseUrl}/api/sessions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
    const data = await response.json();
    return data.id;
  }
}

// Usage
const client = new LocalInferenceClient();

// Simple chat
const response = await client.chat([{ role: "user", content: "Hello!" }]);
console.log(response);

// Streaming
for await (const chunk of client.streamChat([
  { role: "user", content: "Tell me a story." },
])) {
  process.stdout.write(chunk);
}
```

## Advanced Examples

### Conversation with Memory

```python
import requests

class ConversationManager:
    def __init__(self, session_id: str):
        self.session_id = session_id
        self.base_url = "http://localhost:5000"

    def send(self, message: str, **kwargs) -> str:
        response = requests.post(
            f"{self.base_url}/v1/chat/completions",
            json={
                "session_id": self.session_id,
                "messages": [{"role": "user", "content": message}],
                **kwargs
            }
        )
        return response.json()["choices"][0]["message"]["content"]

    def get_stats(self):
        response = requests.get(
            f"{self.base_url}/api/sessions/{self.session_id}/statistics"
        )
        return response.json()

# Usage
session = requests.post(
    "http://localhost:5000/api/sessions",
    json={"name": "Persistent Chat"}
).json()

chat = ConversationManager(session["id"])

# Conversation continues with full context
print(chat.send("My name is Alice"))
print(chat.send("What's my name?"))  # Remembers "Alice"

# Check token usage
stats = chat.get_stats()
print(f"Used {stats['total_tokens']} tokens across {stats['total_messages']} messages")
```

### Batch Processing

```python
import requests
from concurrent.futures import ThreadPoolExecutor

def process_item(item):
    response = requests.post(
        "http://localhost:5000/v1/chat/completions",
        json={
            "messages": [
                {"role": "system", "content": "Summarize the following text:"},
                {"role": "user", "content": item}
            ],
            "max_tokens": 200
        }
    )
    return response.json()["choices"][0]["message"]["content"]

items = [
    "Long text to summarize 1...",
    "Long text to summarize 2...",
    "Long text to summarize 3..."
]

with ThreadPoolExecutor(max_workers=3) as executor:
    results = list(executor.map(process_item, items))
```
