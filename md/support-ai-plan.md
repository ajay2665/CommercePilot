# Support AI — Build Plan (Build-First Pod)

## What it does
Customer support + email/chat filtering & triage. Classifies incoming email intent (brand, category, urgency, confidence), routes to the right team via rules, drafts replies, flags escalations, notifies Slack. This is InboxIQ's engine.

## Data it owns
- `Tickets` (id, brand, category, urgency, confidence, subject, body, sender, status, assigned_team, created_at)
- `RoutingRules` (id, brand, category, urgency_threshold, target_team, slack_channel)

## Tech stack
- **Gmail API** (OAuth2, polling via `BackgroundService`) — replaces n8n
- **FastEndpoints** on ASP.NET Core — single API, read + write
- **EF Core + SQL Server** — persistence
- **LLM call** (your MS Agent Framework `[MessageHandler]` / `RunStreamingAsync` pattern) — classification
- **Slack Incoming Webhook** — escalation + save notifications
- **Next.js/React** — dashboard (ticket table, filters by team/status/urgency)

## Build steps

### 1. Google Cloud setup (30 min)
- Create GCP project, enable Gmail API
- OAuth consent screen → External → add yourself as test user → **Publishing status: Production** (avoids 7-day token expiry; click through unverified warning since it's single-account internal use)
- Generate OAuth client credentials (Desktop app type), run one-time auth flow to get a refresh token, store it (user-secrets locally / Key Vault later)

### 2. SQL Server schema + FastEndpoints scaffold (Mon)
- `Tickets`, `RoutingRules` tables
- FastEndpoints project skeleton, EF Core DbContext wired up

### 3. Gmail polling service (Tue AM)
- `BackgroundService` polling `users.messages.list` (query: `is:unread in:inbox`) every 60–120s
- Track `historyId` to avoid reprocessing; mark message read/labeled after ticket created
- On new message → call internal classify logic directly (no webhook hop needed now that it's in-process) or `POST /api/triage` if you want it decoupled

### 4. Classify & route endpoint (Tue PM–Wed)
- `POST /api/triage`: LLM prompt returns `{brand, category, urgency, confidence}`
- Look up `RoutingRules`, decide `target_team` + `slack_channel`
- Write ticket via EF Core
- Return result

### 5. Slack notification + escalation branch (Thu)
- On save: post to routed Slack channel
- If `urgency=high` AND `confidence<threshold`: separate escalation channel/message

### 6. Dashboard (Fri)
- `GET /api/tickets?team=&status=&urgency=`
- Next.js table + filter UI, CORS enabled for the Next.js origin
- Simple API key/bearer token on the API for the demo

### 7. End-to-end test + polish (Sat–Sun)
- Test across all 5 brands, real inbox emails
- Refine classification prompt on misfires
- Demo script + backup recording

## Integration points with other pods
- Feeds **Analytics AI**: ticket volume/urgency by brand becomes a BI source
- Sits under **AI Orchestrator** eventually — for now, LLM calls happen in-process, no separate agent bus needed yet

## Definition of done
- New email in Gmail inbox → ticket appears in SQL Server within ~2 min → correct Slack channel notified → visible in dashboard with working filters, end to end, all 5 brands.
