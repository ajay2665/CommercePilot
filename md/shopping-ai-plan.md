# Shopping AI — Build Plan

## What it does
Product discovery, personalized recommendations, cart recovery. Customer-facing storefront intelligence. Build this last — it needs Inventory AI (stock) and Pricing AI (live prices) to be meaningful; without them it's recommending items that may be out of stock or wrongly priced.

## Data it owns
- `CustomerEvents` (customer_id, sku, event_type: view/cart/purchase, timestamp)
- `Recommendations` (customer_id, sku, score, generated_at)
- `AbandonedCarts` (cart_id, customer_id, skus, last_active_at, recovery_email_sent)

## Tech stack
- Same **SQL Server + FastEndpoints** service
- Recommendation logic: start with simple collaborative filtering or "frequently bought together" (SQL aggregation), not an LLM — cheaper and more predictable; LLM optional later for natural-language product search
- Cart recovery: reuse the Gmail/email-sending pattern already built for Support AI, just outbound instead of inbound

## Build steps
1. `CustomerEvents` table + event-tracking hook in the storefront (view/cart/purchase)
2. Simple recommendation job: "customers who bought X also bought Y" query, written to `Recommendations`
3. `GET /api/shopping/recommendations?customer_id=` for the storefront to call
4. Abandoned cart job: carts inactive >N hours → trigger recovery email
5. Once Pricing/Inventory AI exist, join recommendations against live stock + price so nothing out-of-stock or mispriced gets recommended

## Integration points with other pods
- **Consumes**: Inventory AI (stock), Pricing AI (live price) — hard dependency, build after those
- **Feeds**: Analytics AI (conversion/recommendation-performance KPIs)

## Definition of done
- Storefront can call the recommendations endpoint and get stock-aware, correctly-priced suggestions; abandoned cart emails send automatically.
