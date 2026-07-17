# Warehouse AI — Build Plan

## What it does
Fulfillment ops, packing/shipping automation, 3PL coordination. This is *movement* — distinct from Inventory AI's *forecasting*. Triggered by orders, consumes stock data.

## Data it owns
- `Shipments` (id, order_id, carrier, tracking_number, status, shipped_at)
- `FulfillmentQueue` (order_id, sku, qty, status: pending/picked/packed/shipped)

## Tech stack
- Same **SQL Server + FastEndpoints** service
- 3PL/carrier integration: carrier API (e.g., ShipStation, EasyPost, or the 3PL's own API) — pick one before building, this is the real dependency
- No LLM needed for the core flow; LLM only useful later for exception-handling (e.g., drafting a customer note on a delayed shipment)

## Build steps
1. Confirm which 3PL/carrier API you're integrating with per brand
2. `Shipments`, `FulfillmentQueue` tables
3. Order-received webhook (from e-commerce platform, same source Inventory AI syncs from) → creates `FulfillmentQueue` entries
4. Carrier API integration: create shipment, get tracking number, update `Shipments`
5. Status webhook/polling from carrier → update shipment status → notify customer (reuse Support AI's ticket/Slack pattern for exceptions like failed delivery)

## Integration points with other pods
- **Consumes**: Inventory AI (`Products.current_stock` — don't queue fulfillment for out-of-stock items), order events from brand e-commerce platforms
- **Feeds**: Analytics AI (fulfillment time KPIs), Support AI (auto-flag delayed shipments as tickets)

## Definition of done
- An order creates a fulfillment queue entry, gets a real tracking number via the carrier API, and status updates flow back into the system automatically.
