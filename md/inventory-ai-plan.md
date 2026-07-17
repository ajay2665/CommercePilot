# Inventory AI — Build Plan

## What it does
Demand forecasting, cross-brand stock reconciliation. This is *forecasting*, not fulfillment — Warehouse AI handles physical movement. Feeds Pricing AI and Warehouse AI.

## Data it owns
- `Products` (id, brand, sku, name, current_stock, reorder_point)
- `StockMovements` (id, sku, delta, reason, source, timestamp)
- `DemandForecast` (sku, week_start, predicted_units, confidence)

## Tech stack
- Same **SQL Server + FastEndpoints** service, new endpoint group
- Data source: your existing e-commerce/POS platform's API (Shopify, WooCommerce, etc. — confirm per brand) for current stock + sales history
- Forecasting: start simple — moving average / exponential smoothing in C# or a small Python job; LLM only for anomaly explanation, not the core forecast math

## Build steps
1. Confirm the actual source system(s) for stock data per brand (this varies — get API access first, this is the real dependency)
2. `Products`, `StockMovements` tables + nightly sync job pulling stock levels from each brand's platform
3. Basic forecast job (weekly moving average per SKU) writing to `DemandForecast`
4. `GET /api/inventory/products`, `GET /api/inventory/forecast?sku=` endpoints
5. Low-stock alert: reuse Support AI's Slack webhook pattern for reorder-point breaches

## Integration points with other pods
- **Feeds**: Pricing AI (stock levels inform dynamic pricing), Warehouse AI (fulfillment needs current stock), Analytics AI (stock-out KPIs)
- **Consumes**: brand e-commerce platform APIs (external, not another pod)

## Definition of done
- Stock levels for at least one brand sync automatically and are queryable via API; a basic forecast exists per SKU; low-stock Slack alert fires correctly.
