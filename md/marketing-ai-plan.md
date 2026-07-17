# Marketing AI — Build Plan

## What it does
Content, ads, SEO, influencer/UGC sourcing, campaign automation. Largely independent of the other pods — can be built in parallel once core infra (API + SQL Server) exists.

## Data it owns
- `Campaigns` (id, brand, channel, status, budget, start_date, end_date)
- `ContentAssets` (id, brand, type, status, generated_by_ai, published_at)
- `SEOTracking` (brand, keyword, rank, checked_at)

## Tech stack
- Same **SQL Server + FastEndpoints** service
- LLM for content drafting (copy, ad variants, SEO keyword suggestions) — reuse the same agent pattern as Support AI's classifier, different prompt
- Publishing: platform APIs vary by channel (Meta Ads, Google Ads, etc.) — scope which channels matter first before building publishing automation; content *generation* can ship well before *auto-publishing*

## Build steps
1. Start with content generation only, no auto-publish: `POST /api/marketing/generate-content` → LLM drafts copy/ad variants → human reviews and manually posts
2. `Campaigns`, `ContentAssets` tables
3. Dashboard page listing draft content awaiting approval, per brand
4. SEO tracking: simple weekly rank-check job (via a rank-tracking API) writing to `SEOTracking`
5. Only once the above is solid, evaluate auto-publishing to specific channels (bigger scope, needs each platform's API + approval flow)

## Integration points with other pods
- **Feeds**: Analytics AI (campaign performance KPIs)
- Loosely coupled to everything else — safe to build in parallel

## Definition of done
- Can generate on-brand draft content/ad copy for a given brand via API, reviewable in a dashboard, with a working SEO rank-tracking job.
