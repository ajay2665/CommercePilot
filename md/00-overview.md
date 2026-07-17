# Atlas — Build Plan Index

Atlas is the umbrella ecosystem. **Commerce Brain** (shared data/knowledge layer) and **AI Orchestrator** (agent-to-agent routing/security) sit underneath all 7 pods. Each pod plan below assumes those two are eventually shared infrastructure, but can be built and demoed standalone first.

## Architecture recap (current, no third-party data store)

```
Gmail API (poll)  →  FastEndpoints API  →  SQL Server
                         │      │
                         │      └─→ Slack webhook
                         │
                 Next.js Dashboard  ←  GET /api/tickets
```

One ASP.NET Core + FastEndpoints service handles both writes (triage pipeline) and reads (dashboard). n8n is removed; Gmail polling lives inside the API as a background service.

## Pod build order (recommended)

| Order | Pod | Why this order |
|---|---|---|
| 1 | **Support AI** | Fastest to build, reuses existing triage pattern, becomes the client demo |
| 2 | Analytics AI | Needs Support AI's data to have something to roll up |
| 3 | Inventory AI | Foundational for Pricing/Warehouse |
| 4 | Pricing AI | Depends on Inventory data |
| 5 | Warehouse AI | Depends on Inventory data |
| 6 | Marketing AI | Independent, can run in parallel once core infra exists |
| 7 | Shopping AI | Customer-facing, needs Inventory + Pricing to be meaningful |

## Files in this set

- `support-ai-plan.md` — build first, full detail, this week's target
- `analytics-ai-plan.md`
- `inventory-ai-plan.md`
- `pricing-ai-plan.md`
- `warehouse-ai-plan.md`
- `marketing-ai-plan.md`
- `shopping-ai-plan.md`

Each file follows the same structure: **What it does → Data it owns → Tech stack → Build steps → Integration points with other pods → Definition of done.**
