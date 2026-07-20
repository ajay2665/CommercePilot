# StorePilot

AI-first commerce platform — Support, Inventory and Shopping intelligence as vertical pods on one FastEndpoints modular monolith. **Fully local:** SQL Server LocalDB + Ollama, no Docker, no cloud keys.

Architecture and roadmap: [md/01-architecture.md](md/01-architecture.md) · Pod plans: [md/00-overview.md](md/00-overview.md)

## Status

| Phase | Scope | State |
|---|---|---|
| 0 | Solution scaffold, EF migrations, event bus, LLM factory, Next.js shell | ✅ done |
| 1a | Support AI core: intake → LLM triage → routing → notifications → dashboard | ✅ done |
| 2 | Inventory AI: forecasts (moving avg), health score, predictive alerts, reorder engine, copilot | ✅ done |
| 3 | Shopping AI: semantic search (nomic embeddings), co-purchase recommendations, trending, cart recovery, RAG assistant, compare | ✅ done |
| 1b | Support draft replies + human approval (HITL), KB RAG chat page, AI usage page | next |
| 4 | JWT/RBAC, Slack/Gmail/Shopify toggles, RabbitMQ/Redis at deployment | planned |

Pod pages: **/inventory** (health, alerts, reorders, product table) · **/inventory/forecast** (chart) · **/inventory/copilot** · **/shopping** (semantic search, recommendations, trending) · **/shopping/assistant** (RAG with cited sources).

## Run it

Prereqs (already set up on this machine): .NET 9 SDK, Node 20+, SQL Server LocalDB, [Ollama](https://ollama.com) with `llama3.1:8b` pulled.

```powershell
# 1. API — http://localhost:5080 (swagger at /swagger); migrates + seeds on first run
dotnet run --project src/StorePilot.Api

# 2. Dashboard — http://localhost:3000
cd web; npm run dev
```

Open the dashboard → **Support AI** → **Submit ticket** (sample buttons provided). The ticket is queued, classified by the local LLM (brand/category/urgency/sentiment/confidence), routed via `RoutingRules`, and the team notification appears under the bell icon. First classification after a reboot loads the model into RAM (~1–3 min); afterwards it's seconds.

```powershell
# Or submit programmatically:
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/support/triage `
  -ContentType "application/json" `
  -Body '{"subject":"Double charge","body":"I was charged twice for my Luma Beauty order.","sender":"a@example.com"}'
```

## Layout

```
src/StorePilot.Api             FastEndpoints host — feature slices (REPR), workers, composition root
src/StorePilot.Application     Use cases: commands/queries, triage pipeline, events, prompts, options
src/StorePilot.Domain          Entities + enums, zero dependencies
src/StorePilot.Infrastructure  EF Core (LocalDB), event bus, work queue, Ollama client, seeder
src/StorePilot.Shared          Cross-cutting primitives
tests/StorePilot.UnitTests     Routing precedence + classification parser tests
web/                         Next.js 15 dashboard (Tailwind, TanStack Query)
md/                          Architecture doc + pod build plans
```

## Config toggles (appsettings.json)

Local-first by default; cloud services are opt-in (`archtecture.txt` decision 10):

- `Llm` — `ollama` (default, free/local) | `openai` (wired) | `gemini` (stub). To use your GPT key, put this in git-ignored `src/StorePilot.Api/appsettings.Development.json` (or set `OPENAI_API_KEY`) and restart:

  ```json
  "Llm": {
    "Provider": "openai",
    "ChatModel": "gpt-5-mini",
    "EmbeddingModel": "text-embedding-3-small",
    "ApiKey": "sk-…",
    "InputCostPer1M": 0.25,
    "OutputCostPer1M": 2.0
  }
  ```

  Embeddings re-ingest automatically whenever the embedding model changes (vectors from different models aren't comparable; both providers are pinned to 768 dims). Cost per call is computed from the token counts × the `…CostPer1M` values into `AiInteractions`. Switching back to ollama re-ingests again.
- `Intake` — `form` (default) | `gmail`
- `Notifications` — `local` (default) | `slack`
- `Auth:ApiKey` — empty = open local dev; set a value to require `Authorization: Bearer <key>`
