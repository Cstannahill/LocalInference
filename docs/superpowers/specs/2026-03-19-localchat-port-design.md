# Design Spec: Porting LocalChat Infrastructure to LocalInference

**Status:** Draft
**Date:** 2026-03-19
**Topic:** Infrastructure Porting & Generalization

## 1. Overview
This project involves porting core infrastructure features from the `LocalChat` project into `LocalInference`. The goal is to provide `LocalInference` with robust, enterprise-grade backend capabilities for memory management, RAG, and multi-provider abstraction while stripping away roleplay-specific hardcoding.

## 2. Generalized Entity Mapping
To transform "Roleplay" features into generalized API features, we will perform the following renames:

| LocalChat Entity | LocalInference Name | Purpose |
| :--- | :--- | :--- |
| `Character` / `Persona` | `SystemProfile` | Agent configuration: system prompts, inference parameters, and default model. |
| `Lorebook` | `ReferenceData` | Knowledge bases (PDFs, Markdown) for RAG. |
| `MemoryItem` | `ExtractedKnowledge` | Persistent facts extracted from conversation history. |
| `Conversation` | `Session` | Stateful chat container with history and context. |
| `Scenario` | `ExecutionContext` | Defines the current problem statement or task context. |

## 3. The Data Layer
We will implement a relational database schema (EF Core) that supports both metadata and vector storage.

### Core Tables:
- **SystemProfiles:** Stores agent identity and default settings.
- **ReferenceData:** Container for vectorized knowledge sets.
- **ReferenceDataItems:** Chunks of data with associated embedding vectors (Stored in **Npgsql/pgvector** for production or **SQLite** for local development).
- **ExtractedKnowledge:** Long-term memory facts extracted via background tasks.
- **Sessions & Messages:** Extended to support `SystemProfile` links and `PromptSnapshot` logging.

## 4. The Memory Pipeline & RAG Engine
A **Budget-Based Context Composer** will manage the flow of data into the inference prompt.

### Context Budgeting Strategy:
1. **System Slice (High Priority):** Instructions from `SystemProfile`.
2. **Retrieval Slice (Medium Priority):** Relevant chunks from `ReferenceData` (Vector Search).
3. **Long-Term Slice (Medium Priority):** `ExtractedKnowledge` (Facts from past sessions).
4. **History Slice (Medium Priority):** Recent message turns from the `Session`.
5. **Summary Slice (Medium Priority):** Condensed history block.

### Retrieval:
- Multi-source vector search across `ReferenceData` and `ExtractedKnowledge`.
- Results are ranked and filtered by relevance before insertion into the context budget.

## 5. Threshold-Based Summarization
Summarization is handled by the **Background Worker** to ensure non-blocking inference.

### Trigger Logic:
- **Checkpoint-Based:** Every 10 messages, a "Mini-Summary" is generated.
- **Threshold-Based (Requirement):** When the active context exceeds **85%** of the model's limit:
    - The worker condenses the oldest 50% of messages into a "Historical Context Block".
    - This block replaces the messages in the **Summary Slice**, ensuring they are still present for context without bloating the window.
    - Raw messages are offloaded from the active memory but remain searchable via RAG.

## 6. Provider Abstraction Layer
A unified interface (`IInferenceProvider`) will handle communication with multiple providers:
- **Ollama:** Local inference.
- **OpenRouter/OpenAI/Anthropic:** Cloud-based inference.
- **Model Profiles:** Each model defines its `MaxContextLimit` and `OptimalTokenUsage`.

## 7. Implementation Plan
1. **Infrastructure Foundation:** Implement DB models and migrations.
2. **Context Core:** Port the `PromptComposer` and budget allocation logic.
3. **Background Services:** Implement the `SummaryService` and `KnowledgeExtractor`.
4. **RAG Engine:** Integrate vector search and `ReferenceData` management.
5. **API Endpoints:** Expose `SystemProfile` and `ReferenceData` CRUD while updating `/chat` to use the new pipeline.
