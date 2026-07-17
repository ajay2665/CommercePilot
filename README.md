# CommercePilot

AI-first commerce platform — Support, Inventory and Shopping intelligence as vertical pods on one FastEndpoints modular monolith. **Fully local:** SQL Server LocalDB + Ollama, no Docker, no cloud keys.

Architecture and roadmap: [md/01-architecture.md](md/01-architecture.md) · Pod plans: [md/00-overview.md](md/00-overview.md)

## Status

| Phase | Scope | State |
|---|---|---|
| 0 | Solution scaffold, EF migrations, event bus, LLM factory, Next.js shell | ✅ done |
| 1a | Support AI core: intake → LLM triage → routing → notifications → dashboard | ✅ done |
| 1b | Draft replies + human approval (HITL), KB RAG chat, AI usage page | next |
| 2 | Inventory AI (sync/seed, forecasts, alerts, copilot) | planned |
| 3 | Shopping AI (events, recommendations, semantic search, assistant) | planned |

## Run it

Prereqs (already set up on this machine): .NET 9 SDK, Node 20+, SQL Server LocalDB, [Ollama](https://ollama.com) with `llama3.1:8b` pulled.

```powershell
# 1. API — http://localhost:5080 (swagger at /swagger); migrates + seeds on first run
dotnet run --project src/Commerce.Api

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
src/Commerce.Api             FastEndpoints host — feature slices (REPR), workers, composition root
src/Commerce.Application     Use cases: commands/queries, triage pipeline, events, prompts, options
src/Commerce.Domain          Entities + enums, zero dependencies
src/Commerce.Infrastructure  EF Core (LocalDB), event bus, work queue, Ollama client, seeder
src/Commerce.Shared          Cross-cutting primitives
tests/Commerce.UnitTests     Routing precedence + classification parser tests
web/                         Next.js 15 dashboard (Tailwind, TanStack Query)
md/                          Architecture doc + pod build plans
```

## Config toggles (appsettings.json)

Local-first by default; cloud services are opt-in (`archtecture.txt` decision 10):

- `Llm` — `ollama` (default) | `openai` | `gemini`
- `Intake` — `form` (default) | `gmail`
- `Notifications` — `local` (default) | `slack`
- `Auth:ApiKey` — empty = open local dev; set a value to require `Authorization: Bearer <key>`
