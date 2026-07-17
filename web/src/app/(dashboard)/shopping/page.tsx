"use client";

import { useState } from "react";
import Link from "next/link";
import { MessageCircleQuestion, Search } from "lucide-react";
import { useCustomers, useRecommendations, useShoppingSearch, useTrending } from "@/features/shopping/hooks";
import type { ShopProductCard } from "@/lib/api";

function ProductCard({ card }: { card: ShopProductCard }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-2">
        <p className="font-medium leading-snug text-slate-800">{card.name}</p>
        <span
          className={`shrink-0 rounded-full px-2 py-0.5 text-[11px] font-medium ${
            card.inStock ? "bg-emerald-100 text-emerald-700" : "bg-red-100 text-red-700"
          }`}
        >
          {card.inStock ? `${card.currentStock} in stock` : "out of stock"}
        </span>
      </div>
      <p className="text-xs text-slate-400">
        {card.brand} · {card.sku}
      </p>
      <div className="mt-2 flex items-center justify-between">
        <span className="text-lg font-semibold text-slate-900">€{card.price.toFixed(2)}</span>
        {card.reason && <span className="max-w-[60%] truncate text-[11px] text-slate-400">{card.reason}</span>}
      </div>
    </div>
  );
}

function CardGrid({ cards, empty }: { cards?: ShopProductCard[]; empty: string }) {
  if (!cards) return <p className="py-6 text-center text-sm text-slate-400">Loading…</p>;
  if (cards.length === 0) return <p className="py-6 text-center text-sm text-slate-400">{empty}</p>;
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {cards.map((c) => (
        <ProductCard key={c.productId} card={c} />
      ))}
    </div>
  );
}

export default function ShoppingPage() {
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");
  const [customerId, setCustomerId] = useState<string | null>(null);

  const search = useShoppingSearch(query);
  const { data: trending } = useTrending();
  const { data: customers } = useCustomers();
  const recommendations = useRecommendations(customerId);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Shopping AI</h1>
          <p className="text-sm text-slate-500">
            Semantic search (nomic embeddings), co-purchase recommendations, live stock awareness.
          </p>
        </div>
        <Link
          href="/shopping/assistant"
          className="flex items-center gap-2 rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
        >
          <MessageCircleQuestion size={15} /> AI Assistant
        </Link>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <p className="mb-3 text-sm font-semibold text-slate-700">Semantic product search</p>
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && setQuery(input)}
              placeholder='Try "something to keep drinks cold on a hike" or "gift for a vinyl lover"…'
              className="w-full rounded-lg border border-slate-200 py-2.5 pl-9 pr-3 text-sm outline-none focus:border-slate-400"
            />
          </div>
          <button
            onClick={() => setQuery(input)}
            disabled={input.trim().length < 2}
            className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-40"
          >
            Search
          </button>
        </div>
        {query && (
          <div className="mt-4">
            {search.isFetching ? (
              <p className="py-6 text-center text-sm text-slate-400">Embedding query & ranking…</p>
            ) : (
              <CardGrid cards={search.data} empty="No matches." />
            )}
          </div>
        )}
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <div className="mb-3 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">Recommendations</p>
          <select
            value={customerId ?? ""}
            onChange={(e) => setCustomerId(e.target.value || null)}
            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm outline-none focus:border-slate-400"
          >
            <option value="">Pick a customer…</option>
            {customers?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name} ({c.email})
              </option>
            ))}
          </select>
        </div>
        {customerId ? (
          <CardGrid cards={recommendations.data} empty="No recommendations yet — rebuild runs nightly." />
        ) : (
          <p className="py-6 text-center text-sm text-slate-400">
            Select a customer to see &quot;bought together&quot; recommendations joined against live stock.
          </p>
        )}
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        <p className="mb-3 text-sm font-semibold text-slate-700">Trending (last 30 days)</p>
        <CardGrid cards={trending} empty="No sales in window." />
      </div>
    </div>
  );
}
