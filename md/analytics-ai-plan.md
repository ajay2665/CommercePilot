# Analytics AI — Build Plan

## What it does
Cross-brand BI, executive dashboards, cash flow & sales forecasting. Rolls up all 5 brands into one CEO-level view. Consumes data from every other pod rather than owning a primary source itself.

## Data it owns
- `MetricsSnapshot` (date, brand, metric_name, value) — a generic time-series rollup table
- Materialized/aggregate views over `Tickets` (Support AI), `Orders`/`Sales` (Shopping AI), `StockLevels` (Inventory AI) once those exist

## Tech stack
- Same **SQL Server** instance (or a read replica later if load grows)
- **FastEndpoints** — new `GET /api/analytics/*` endpoints on the same API service
- **Next.js/React** — exec dashboard, charts (recharts)
- Scheduled aggregation job (`BackgroundService`, nightly) that rolls raw pod tables into `MetricsSnapshot`

## Build steps
1. Define the first 5–8 KPIs the CEO actually wants (e.g., tickets/day by brand, avg resolution time, revenue by brand, stock-out rate) — confirm with client before building
2. Build `MetricsSnapshot` table + nightly aggregation job pulling from Support AI's `Tickets` table (only real data source at this stage)
3. `GET /api/analytics/kpis?brand=&range=` endpoint
4. Dashboard page: cards for headline numbers + trend charts
5. As Inventory/Shopping AI come online, extend the aggregation job to pull their tables in too

## Integration points with other pods
- **Consumes**: Support AI (live now), Inventory/Pricing/Shopping AI (later)
- **Produces**: nothing consumed downstream — this is the terminal/reporting layer

## Definition of done
- CEO dashboard shows real ticket-volume and urgency trends per brand, refreshed nightly, with room to plug in more KPIs as other pods ship data.
