# LocalChat to LocalInference Infrastructure Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port core infrastructure (Memory, RAG, Background Tasks, Provider Abstraction) from LocalChat to LocalInference while generalizing entities.

**Architecture:** Clean Architecture with EF Core for persistence and a Budget-Based Context Composer for memory management. Threshold-based summarization (85%) ensures efficient context usage.

**Tech Stack:** .NET 9, EF Core, Npgsql/pgvector, Ollama/OpenRouter.

---

### Task 1: Foundation - Generalized Data Layer

**Files:**
- Create: `src/LocalInference.Domain/Entities/SystemProfile.cs`
- Create: `src/LocalInference.Domain/Entities/ReferenceData.cs`
- Create: `src/LocalInference.Domain/Entities/ReferenceDataItem.cs`
- Modify: `src/LocalInference.Infrastructure/Persistence/ApplicationDbContext.cs`

- [ ] **Step 1: Define SystemProfile Entity**
    ```csharp
    public class SystemProfile : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public float Temperature { get; set; } = 0.7f;
        public int MaxContextTokens { get; set; } = 8192;
        public string DefaultModel { get; set; } = "llama3";
        public ICollection<ReferenceData> LinkedReferenceSets { get; set; } = new List<ReferenceData>();
    }
    ```
- [ ] **Step 2: Define ReferenceData Entities**
    ```csharp
    public class ReferenceData : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<ReferenceDataItem> Items { get; set; } = new List<ReferenceDataItem>();
    }

    public class ReferenceDataItem : Entity
    {
        public string Content { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public Guid ReferenceDataId { get; set; }
    }
    ```
- [ ] **Step 3: Update ApplicationDbContext**
    Add `DbSet<SystemProfile>`, `DbSet<ReferenceData>`, and `DbSet<ReferenceDataItem>`. Configure relationships and pgvector indexing for embeddings.
- [ ] **Step 4: Create and Run Migration**
    Run: `dotnet ef migrations add AddGeneralizedInfrastructure --project src/LocalInference.Infrastructure --startup-project src/LocalInference.Api`
    Expected: Migration file generated with new tables.
- [ ] **Step 5: Commit**
    ```bash
    git add src/LocalInference.Domain/Entities/ src/LocalInference.Infrastructure/
    git commit -m "feat: implement generalized data layer foundation"
    ```

---

### Task 2: Context Management - Budget-Based Composer

**Files:**
- Create: `src/LocalInference.Application/Prompting/ContextComposer.cs`
- Create: `src/LocalInference.Application/Prompting/ContextBudget.cs`

- [ ] **Step 1: Implement ContextBudget logic**
    Define budget allocations for System, Retrieval, History, and Summary slices.
- [ ] **Step 2: Implement ContextComposer**
    Logic to assemble the final prompt based on priorities and available tokens.
- [ ] **Step 3: Write Unit Test for Composition**
    Test that higher priority slices (System) are preserved when the budget is tight.
- [ ] **Step 4: Commit**
    ```bash
    git add src/LocalInference.Application/Prompting/
    git commit -m "feat: implement budget-based context composer"
    ```

---

### Task 3: Background Worker - Threshold Summarization (85%)

**Files:**
- Create: `src/LocalInference.Application/Services/BackgroundSummarizationService.cs`
- Create: `src/LocalInference.Application/Jobs/SummarizationJob.cs`

- [ ] **Step 1: Implement Threshold Check**
    ```csharp
    public bool ShouldSummarize(int currentTokens, int maxTokens)
        => currentTokens >= (maxTokens * 0.85);
    ```
- [ ] **Step 2: Implement Summarization Job**
    Logic to take the oldest 50% of history and condense it using an LLM call.
- [ ] **Step 3: Register Background Service**
    Update `DependencyInjection` to include the summarization worker.
- [ ] **Step 4: Commit**
    ```bash
    git add src/LocalInference.Application/Services/ src/LocalInference.Application/Jobs/
    git commit -m "feat: implement 85% threshold-based summarization"
    ```

---

### Task 4: RAG Engine - Multi-Source Retrieval

**Files:**
- Create: `src/LocalInference.Application/Services/RetrievalService.cs`
- Modify: `src/LocalInference.Infrastructure/Retrieval/TechnicalRetrievalService.cs`

- [ ] **Step 1: Implement Vector Search**
    Logic to search across `ReferenceDataItems` using cosine similarity.
- [ ] **Step 2: Integrate with ContextComposer**
    Ensure retrieval results are passed into the "Retrieval Slice" of the budget.
- [ ] **Step 3: Commit**
    ```bash
    git add src/LocalInference.Application/Services/ src/LocalInference.Infrastructure/Retrieval/
    git commit -m "feat: implement multi-source RAG engine"
    ```

---

### Task 5: API Integration & Finalization

**Files:**
- Create: `src/LocalInference.Api/Endpoints/SystemProfileEndpoints.cs`
- Modify: `src/LocalInference.Api/Endpoints/ChatCompletionsEndpoints.cs`

- [ ] **Step 1: Add SystemProfile CRUD Endpoints**
- [ ] **Step 2: Update Chat Endpoint**
    Modify `/chat` to resolve the `SystemProfile`, compose context via `ContextComposer`, and check summarization thresholds.
- [ ] **Step 3: Integration Test**
    Send a message to a session and verify the response includes relevant data from a linked `ReferenceData` set.
- [ ] **Step 4: Commit**
    ```bash
    git add src/LocalInference.Api/Endpoints/
    git commit -m "feat: expose system profile endpoints and update chat pipeline"
    ```
