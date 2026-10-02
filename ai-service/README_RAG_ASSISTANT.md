# SurplusLink AI Assistant & RAG Knowledge Engine

## Architecture Overview
The SurplusLink AI Assistant is a production-ready RAG (Retrieval-Augmented Generation) and tool-using conversational assistant built into the SurplusLink ecosystem.

```
Flutter Mobile / React Web
         ↓
ASP.NET Core API (POST /api/ai/chat)
         ↓ (x-internal-token + authenticated claims)
Python FastAPI Service (POST /internal/chat)
         ↓
 ┌─────────────────────────────────────────────────────────┐
 │ SurplusLink Assistant Engine                            │
 │                                                         │
 │ • Intent & Multilingual Router (EN, SI, TA, Romanized)  │
 │ • RAG Knowledge Retriever (Local Vector Store)          │
 │ • Requirement Draft Agent (Catalog & Package Aware)     │
 │ • Match Explanation Agent (Persisted Facts & Scores)    │
 │ • Allow-listed Backend Tools Client                     │
 │ • Groq LLM Provider                                     │
 └─────────────────────────────────────────────────────────┘
         ↓ (GET /api/ai-internal/tools/*)
ASP.NET Core Business Logic & Authorization Authority
         ↓
PostgreSQL Database
```

## Security & Architectural Guarantees
1. **Database & Service Isolation**: Clients and the Python AI service NEVER query PostgreSQL directly. ASP.NET Core remains the sole authority for authentication, user roles, listings, requirements, offers, and transactions.
2. **Strict Identity Context**: The Python AI service cannot accept arbitrary `userId` parameters to access other users' data. Identity is strictly passed from authenticated claims.
3. **No Silently Submitted Actions**: The AI Assistant NEVER automatically submits buyer requirements, approves listings, or alters stock. Requirements are extracted into a structured `RequirementDraft` for explicit user review and confirmation.
4. **Prompt Injection Defense**: Knowledge chunks and tool outputs are treated as data, preventing prompt injection attacks from granting unauthorized tool or data access.

---

## Environment Configuration
Set the following environment variables in your deployment environment or `.env` file:

```env
# Groq LLM Provider Config
GROQ_API_KEY=your_groq_api_key_here
GROQ_MODEL=llama-3.3-70b-versatile
GROQ_TIMEOUT=15.0

# Service Shared Secret Token
AI_SERVICE_SHARED_TOKEN=development-shared-token-32-chars-long

# Backend API Endpoints
AI_SERVICE_BASE_URL=http://localhost:5000
ASP_NET_BASE_URL=http://localhost:5170
```

---

## RAG Knowledge Base Structure
Knowledge source documents are stored under `ai-service/knowledge/`:
- `surpluslink/`: Core platform overview, buyer guide, seller guide, manager guide, matching workflow, quantity model, transaction flow, safety & policy.
- `sri_lanka_construction/`: Material guides for Sri Lanka (cement, paint, tiles, timber, steel, PVC, aggregates, sealants, tools/equipment, packaging/units, reuse guidelines, storage, safety, sustainability).

### Indexing Command
To build or rebuild the local vector store index:

```bash
cd ai-service
python -m app.rag.build_index
```

Output:
```
Loading knowledge documents from: ai-service/knowledge
--- Indexing Summary ---
Documents indexed: 23
Chunks created: 98
Errors: 0
Index persisted to: ai-service/app/rag/data/vector_index.json
```

---

## Allow-Listed Backend Tool Contracts
The Python service invokes internal tool endpoints exposed by `AiInternalToolsController`:
- `get_current_user_summary`
- `get_my_active_listings`
- `get_my_listing`
- `get_my_requirements`
- `get_my_requirement`
- `get_my_matches`
- `get_my_offers`
- `get_my_transactions`
- `get_my_transaction`
- `get_catalog_item`
- `get_material_categories`

---

## Running Tests

### Python AI Service Tests (66 tests):
```bash
cd ai-service
py -m pytest -q
```

### Backend C# Tests (283 tests):
```bash
dotnet test SurplusLink.sln --configuration Release
```

### Flutter Mobile Tests:
```bash
cd mobile
flutter test test/ai_assistant_test.dart
```

---

## Sample Conversations

### 1. English Requirement Draft
> **User**: "yellow paint 10 litres Negombo"  
> **Assistant**:  
> "I've prepared a requirement draft for **Paint**:  
> • Quantity: 10.0 L  
> • Colour: Yellow  
> • Delivery Area: Negombo  
> 
> Your requirement is ready to review. Tap **[ Review Requirement ]** below to double-check and submit."

### 2. Romanized Sinhala Package Calculation
> **User**: "mata 7 cement bags 50kg Negombo walata"  
> **Assistant**:  
> "I've prepared a requirement draft for **Cement**:  
> • Quantity: 7 bags (Total: 350.0 kg)  
> • Delivery Area: Negombo  
> 
> Your requirement is ready to review. Tap **[ Review Requirement ]** below to double-check and submit."

### 3. RAG Knowledge & Storage Query
> **User**: "how should cement be stored"  
> **Assistant**:  
> "Store cement in a dry, enclosed shed. Keep bags at least 15cm off the floor on raised wooden pallets and stack no more than 10 bags high to prevent compaction."  
> *Sources: Cement Products and Storage Guidelines — Storage (Sri Lanka Institute of Architecture & CIDA Technical Standards SLS 107)*
