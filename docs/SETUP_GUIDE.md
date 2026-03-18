# Setup Guide

Complete setup instructions for LocalInference API.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Database Setup](#database-setup)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Running the API](#running-the-api)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **.NET 9.0 SDK** or later
  - Download: https://dotnet.microsoft.com/download
  - Verify: `dotnet --version`

- **PostgreSQL 14+**
  - Download: https://www.postgresql.org/download/
  - Recommended: pgvector extension for optimized vector storage

- **Ollama** (for local inference)
  - Download: https://ollama.ai
  - Or use OpenRouter API key for cloud inference

### System Requirements

- **OS**: Windows 10/11, macOS 10.15+, or Linux
- **RAM**: 8GB minimum (16GB+ recommended for local models)
- **Storage**: 2GB for application + model storage
- **Network**: Internet connection for initial setup

## Database Setup

### 1. Install PostgreSQL

**Windows:**

```powershell
# Using Chocolatey
choco install postgresql

# Or download installer from postgresql.org
```

**macOS:**

```bash
# Using Homebrew
brew install postgresql@14
brew services start postgresql
```

**Linux (Ubuntu/Debian):**

```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
```

### 2. Create Database

```bash
# Switch to postgres user
sudo -u postgres psql

# In PostgreSQL shell:
CREATE DATABASE LocalInference;
CREATE USER inference_user WITH ENCRYPTED PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE LocalInference TO inference_user;
\q
```

### 3. Install pgvector (Optional but Recommended)

```bash
# Clone and build pgvector
git clone --branch v0.5.1 https://github.com/pgvector/pgvector.git
cd pgvector
make
sudo make install

# Enable in database
psql -U postgres -d LocalInference -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

## Installation

### 1. Clone Repository

```bash
git clone <repository-url>
cd LocalInference
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Connection String

Edit `src/LocalInference.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=LocalInference;Username=inference_user;Password=your_password"
  }
}
```

Or use environment variables:

```bash
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=LocalInference;Username=inference_user;Password=your_password"

# Linux/macOS
export ConnectionStrings__DefaultConnection="Host=localhost;Database=LocalInference;Username=inference_user;Password=your_password"
```

### 4. Run Migrations

```bash
cd src/LocalInference.Api
dotnet ef database update
```

If EF Core tools are not installed:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

## Configuration

### Ollama Setup (Local Inference)

1. **Install Ollama:**

   ```bash
   # macOS/Linux
   curl -fsSL https://ollama.ai/install.sh | sh

   # Windows: Download from ollama.ai
   ```

2. **Start Ollama:**

   ```bash
   ollama serve
   ```

3. **Pull a Model:**

   ```bash
   ollama pull llama3.2
   ollama pull nomic-embed-text  # For embeddings
   ```

4. **Verify Ollama:**

   ```bash
   curl http://localhost:11434/api/tags
   ```

5. **Configure API:**
   ```json
   {
     "Inference": {
       "Ollama": {
         "BaseUrl": "http://localhost:11434"
       }
     }
   }
   ```

### OpenRouter Setup (Cloud Inference)

1. **Get API Key:**
   - Sign up at https://openrouter.ai
   - Generate API key

2. **Configure API:**

   ```json
   {
     "Inference": {
       "OpenRouter": {
         "ApiKey": "sk-or-v1-..."
       }
     }
   }
   ```

   Or use environment variable:

   ```bash
   export Inference__OpenRouter__ApiKey="sk-or-v1-..."
   ```

### Full Configuration Example

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "LocalInference": "Debug"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=LocalInference;Username=inference_user;Password=your_password"
  },
  "Inference": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434"
    },
    "OpenRouter": {
      "ApiKey": ""
    }
  }
}
```

## Running the API

### Development Mode

```bash
cd src/LocalInference.Api
dotnet run
```

The API will be available at:

- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001 (if configured)

### Production Mode

```bash
cd src/LocalInference.Api
dotnet run --configuration Release
```

### Docker (Optional)

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocalInference.Api.dll"]
```

Build and run:

```bash
docker build -t localinference .
docker run -p 5000:80 -e ConnectionStrings__DefaultConnection="..." localinference
```

## Verification

### 1. Health Check

```bash
curl http://localhost:5000/health
```

Expected response:

```json
{ "status": "healthy", "timestamp": "2024-01-15T10:00:00Z" }
```

### 2. Create Inference Configuration

First, ensure you have a model pulled in Ollama:

```bash
ollama pull Qwen3.5-2B-UC
```

Create the inference config:

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

### 3. Test Chat Completions Endpoint (Working Example)

```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen3.5-2B-UC",
    "messages": [{"role": "user", "content": "Hello!"}],
    "stream": false
  }'
```

Expected response (first call creates a session):

```json
{
  "id": "chatcmpl-...",
  "object": "chat.completion",
  "created": 1234567890,
  "model": "Qwen3.5-2B-UC",
  "session_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I assist you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 12,
    "total_tokens": 27
  }
}
```

### 4. Enable Logging (Development Debugging)

To see SQL queries and HTTP requests, edit `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "System.Net.Http.HttpClient": "Information"
    }
  }
}
```

### 5. Create and Use Session

```bash
# Create session
SESSION=$(curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Session", "description": "Testing session management"}' | jq -r '.id')

# Use session in follow-up message
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d "{
    \"session_id\": \"$SESSION\",
    \"model\": \"Qwen3.5-2B-UC\",
    \"messages\": [{\"role\": \"user\", \"content\": \"Tell me about yourself\"}],
    \"stream\": false
  }"
```

## Troubleshooting

### Database Connection Issues

**Error:** `NpgsqlException: Connection refused`

**Solutions:**

1. Verify PostgreSQL is running:

   ```bash
   # Windows
   sc query postgresql

   # Linux/macOS
   sudo systemctl status postgresql
   ```

2. Check connection string:
   - Verify host, port, database name
   - Check username/password
   - Ensure user has database privileges

3. Allow network connections:
   Edit `postgresql.conf`:

   ```
   listen_addresses = '*'
   ```

   Edit `pg_hba.conf`:

   ```
   host all all 0.0.0.0/0 md5
   ```

### Ollama Connection Issues

**Error:** `HttpRequestException: Connection refused`

**Solutions:**

1. Verify Ollama is running:

   ```bash
   curl http://localhost:11434/api/tags
   ```

2. Check base URL configuration

3. For Docker deployments, use host networking or expose Ollama port

### Migration Issues

**Error:** `Migration pending`

**Solutions:**

```bash
# Force recreate (WARNING: Data loss)
dotnet ef database drop
dotnet ef database update

# Or create migration if schema changed
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Token Budget Exceeded

**Error:** `TokenBudgetExceededException`

**Solutions:**

1. Increase context window size:

   ```json
   { "context_window_tokens": 16384 }
   ```

2. Enable compression:
   - Automatic via SmartCompression strategy
   - Manual via session statistics endpoint

3. Clear old messages:
   ```bash
   POST /api/sessions/{id}/clear
   ```

### Entity State Management & Concurrency Issues

**Error:** `DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s)`

This error occurs during chat completions when the ORM (Entity Framework Core) tries to save message states but finds the entities don't exist in the expected state.

**Root Causes:**

1. Database migrations not properly applied
2. Concurrent modifications from other processes
3. Stale entity references after long operations

**Solutions:**

1. **Reset the database (development only):**

   ```bash
   cd src/LocalInference.Api
   dotnet ef database drop
   dotnet ef database update
   ```

2. **Verify migrations are applied:**

   ```bash
   dotnet ef migrations list
   ```

3. **Check that Ollama and PostgreSQL are both running**

4. **Ensure fresh session data:**
   - Don't reuse old session IDs if database was reset
   - Create a new session for testing

5. **Enable detailed logging to diagnose:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.EntityFrameworkCore.Database.Command": "Information",
         "Microsoft.EntityFrameworkCore": "Debug"
       }
     }
   }
   ```

The API properly handles entity state by reloading sessions fresh from the database before persistence operations, ensuring optimistic concurrency control works correctly.

### Performance Issues

**Slow Responses:**

1. Check model size - larger models are slower
2. Enable response streaming
3. Optimize context window (smaller = faster)
4. Use GPU acceleration if available

**High Memory Usage:**

1. Reduce concurrent sessions
2. Lower context window sizes
3. Enable message summarization
4. Use checkpoint compression

### Getting Help

1. Check logs:

   ```bash
   dotnet run --verbosity diagnostic
   ```

2. Enable debug logging:

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. Review PostgreSQL logs:
   - Windows: `%PROGRAMDATA%\PostgreSQL\logs`
   - Linux: `/var/log/postgresql`
   - macOS: `/usr/local/var/log/postgres`

## Next Steps

- Read the [API Reference](API_REFERENCE.md)
- Explore [Examples](EXAMPLES.md)
- Review [Architecture](ARCHITECTURE.md)
