# Pricing AI — Build Plan

## What it does
Dynamic pricing, competitor tracking, margin optimization. A cross-brand pricing rules engine — depends on Inventory AI's stock data to make sensible calls (e.g., don't discount something about to stock out).

## Data it owns
- `PriceRules` (id, brand, sku, min_margin_pct, max_discount_pct)
- `CompetitorPrices` (sku, competitor, price, scraped_at)
- `PriceHistory` (sku, price, effective_at, reason)

## Tech stack
- Same **SQL Server + FastEndpoints** service
- Competitor price data: scraping job (respect ToS/robots.txt) or a paid price-tracking API — decide based on budget before building
- LLM used for reasoning/explaining a price recommendation, not for the arithmetic itself (keep margin math deterministic and auditable)

## Build steps
1. Decide competitor-data source (scrape vs. paid API) — this is the real blocker, resolve first
2. `PriceRules`, `CompetitorPrices`, `PriceHistory` tables
3. Nightly job: pull competitor prices → compare against `min_margin_pct`/`max_discount_pct` → propose new price per SKU
4. `POST /api/pricing/approve` — human-in-the-loop approval before a price goes live (don't auto-publish price changes at first)
5. `GET /api/pricing/recommendations` for a review dashboard page

## Integration points with other pods
- **Consumes**: Inventory AI (`Products.current_stock`) to avoid discounting near-stockout items
- **Feeds**: Shopping AI (live prices), Analytics AI (margin trend KPIs)

## Definition of done
- Nightly job produces a reviewable list of price recommendations with rationale, gated behind manual approval before anything changes live pricing.
