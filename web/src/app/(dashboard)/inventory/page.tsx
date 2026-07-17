"use client";

import { useState } from "react";
import Link from "next/link";
import { Check, LineChart, Sparkles } from "lucide-react";
import {
  useAckInventoryAlert,
  useInventoryAlerts,
  useInventoryHealth,
  useInventoryProducts,
  useReorderSuggestions,
} from "@/features/inventory/hooks";
import type { StockClass } from "@/lib/api";

const classStyles: Record<StockClass, string> = {
  fast: "bg-emerald-100 text-emerald-700",
  healthy: "bg-slate-100 text-slate-600",
  slow: "bg-sky-100 text-sky-700",
  lowStock: "bg-red-100 text-red-700",
  dead: "bg-amber-100 text-amber-800",
  overstock: "bg-violet-100 text-violet-700",
};

const classLabel: Record<StockClass, string> = {
  fast: "fast mover",
  healthy: "healthy",
  slow: "slow",
  lowStock: "low stock",
  dead: "dead stock",
  overstock: "overstock",
};

const usd = (v: number) => `$${v.toLocaleString("en-US", { maximumFractionDigits: 0 })}`;

const FILTERS: (StockClass | "all")[] = ["all", "lowStock", "dead", "overstock", "fast", "slow", "healthy"];

export default function InventoryPage() {
  const { data: health } = useInventoryHealth();
  const { data: products } = useInventoryProducts();
  const { data: alerts } = useInventoryAlerts();
  const { data: reorders } = useReorderSuggestions();
  const ack = useAckInventoryAlert();
  const [classFilter, setClassFilter] = useState<StockClass | "all">("all");

  const nameOf = (productId: string) => products?.find((p) => p.productId === productId);
  const countOf = (c: StockClass) => products?.filter((p) => p.classification === c).length ?? 0;
  const filtered = products?.filter((p) => classFilter === "all" || p.classification === classFilter);
  const toggleFilter = (c: StockClass) => setClassFilter((current) => (current === c ? "all" : c));

  const scoreTone =
    (health?.healthScore ?? 0) >= 70 ? "text-emerald-600" : (health?.healthScore ?? 0) >= 45 ? "text-amber-600" : "text-red-600";

  const tiles: { label: string; value: string | number; tone?: string; filter?: StockClass }[] = [
    { label: "Health score", value: health ? `${health.healthScore}/100` : "—", tone: scoreTone },
    { label: "Stock value", value: health ? usd(health.totalStockValue) : "—" },
    { label: "Low stock", value: health?.lowStockCount ?? "—", tone: "text-red-600", filter: "lowStock" },
    { label: "Dead stock", value: health ? `${health.deadCount} · ${usd(health.deadValue)}` : "—", tone: "text-amber-700", filter: "dead" },
    { label: "Overstock", value: health ? `${health.overstockCount} · ${usd(health.overstockValue)}` : "—", tone: "text-violet-700", filter: "overstock" },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Inventory AI — Health</h1>
          <p className="text-sm text-slate-500">
            Moving-average forecasts, stock classification and reorder suggestions — recomputed every 6h.
          </p>
        </div>
        <div className="flex gap-2">
          <Link
            href="/inventory/forecast"
            className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
          >
            <LineChart size={15} /> Forecasts
          </Link>
          <Link
            href="/inventory/copilot"
            className="flex items-center gap-2 rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700"
          >
            <Sparkles size={15} /> Copilot
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-5">
        {tiles.map((t) => (
          <button
            key={t.label}
            disabled={!t.filter}
            onClick={() => t.filter && toggleFilter(t.filter)}
            title={t.filter ? "Click to filter the product table" : undefined}
            className={`rounded-2xl border bg-white p-4 text-left transition-colors ${
              t.filter && classFilter === t.filter
                ? "border-slate-900 ring-1 ring-slate-900"
                : "border-slate-200"
            } ${t.filter ? "cursor-pointer hover:border-slate-400" : "cursor-default"}`}
          >
            <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{t.label}</p>
            <p className={`mt-1 truncate text-2xl font-semibold ${t.tone ?? "text-slate-900"}`}>{t.value}</p>
          </button>
        ))}
      </div>

      {(alerts?.length ?? 0) > 0 && (
        <div className="rounded-2xl border border-slate-200 bg-white">
          <div className="border-b border-slate-100 px-5 py-3 text-sm font-semibold">
            Predictive alerts <span className="ml-1 text-xs font-normal text-slate-400">({alerts!.length} open)</span>
          </div>
          <ul className="divide-y divide-slate-50">
            {alerts!.slice(0, 8).map((a) => (
              <li key={a.id} className="flex items-center gap-3 px-5 py-2.5 text-sm">
                <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${classStyles[a.type === "stockoutRisk" ? "lowStock" : (a.type as StockClass)] ?? "bg-slate-100"}`}>
                  {a.type}
                </span>
                <span className="min-w-0 flex-1 truncate text-slate-700">{a.message}</span>
                <button
                  onClick={() => ack.mutate(a.id)}
                  className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-emerald-600"
                  title="Acknowledge"
                >
                  <Check size={14} />
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      {(reorders?.length ?? 0) > 0 && (
        <div className="rounded-2xl border border-slate-200 bg-white">
          <div className="border-b border-slate-100 px-5 py-3 text-sm font-semibold">Reorder suggestions</div>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-left text-xs uppercase tracking-wide text-slate-400">
                <th className="px-5 py-2 font-medium">Product</th>
                <th className="px-5 py-2 font-medium">Order qty</th>
                <th className="px-5 py-2 font-medium">Order by</th>
                <th className="px-5 py-2 font-medium">Why</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {reorders!.map((r) => (
                <tr key={r.id}>
                  <td className="px-5 py-2.5 font-medium text-slate-800">{nameOf(r.productId)?.name ?? r.productId.slice(0, 8)}</td>
                  <td className="px-5 py-2.5 text-slate-700">{r.quantity}</td>
                  <td className="px-5 py-2.5 text-slate-700">{new Date(r.orderByDate).toLocaleDateString()}</td>
                  <td className="px-5 py-2.5 text-xs text-slate-500">{r.rationale}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex flex-wrap items-center gap-2">
        {FILTERS.map((c) => (
          <button
            key={c}
            onClick={() => setClassFilter(c)}
            className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${
              classFilter === c
                ? "border-slate-900 bg-slate-900 text-white"
                : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
            }`}
          >
            {c === "all" ? `All (${products?.length ?? 0})` : `${classLabel[c]} (${countOf(c)})`}
          </button>
        ))}
        {classFilter !== "all" && (
          <span className="text-xs text-slate-400">
            showing {filtered?.length ?? 0} of {products?.length ?? 0}
          </span>
        )}
      </div>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs uppercase tracking-wide text-slate-400">
              <th className="px-4 py-3 font-medium">Product</th>
              <th className="px-4 py-3 font-medium">Brand</th>
              <th className="px-4 py-3 font-medium">Stock</th>
              <th className="px-4 py-3 font-medium">Reorder pt</th>
              <th className="px-4 py-3 font-medium">Rate/day</th>
              <th className="px-4 py-3 font-medium">30d fcst</th>
              <th className="px-4 py-3 font-medium">Stockout in</th>
              <th className="px-4 py-3 font-medium">Value</th>
              <th className="px-4 py-3 font-medium">Class</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-50">
            {filtered?.length === 0 && (
              <tr>
                <td colSpan={9} className="px-4 py-10 text-center text-slate-400">
                  No products classified as {classFilter === "all" ? "anything" : classLabel[classFilter as StockClass]}.
                </td>
              </tr>
            )}
            {filtered?.map((p) => (
              <tr key={p.productId} className="hover:bg-slate-50/60">
                <td className="px-4 py-2.5">
                  <Link href={`/inventory/forecast?productId=${p.productId}`} className="font-medium text-slate-800 hover:underline">
                    {p.name}
                  </Link>
                  <span className="block text-xs text-slate-400">{p.sku}</span>
                </td>
                <td className="px-4 py-2.5 text-slate-600">{p.brand}</td>
                <td className={`px-4 py-2.5 font-mono ${p.currentStock <= p.reorderPoint ? "font-semibold text-red-600" : "text-slate-700"}`}>
                  {p.currentStock}
                </td>
                <td className="px-4 py-2.5 font-mono text-slate-500">{p.reorderPoint}</td>
                <td className="px-4 py-2.5 font-mono text-slate-700">{p.dailyRate.toFixed(1)}</td>
                <td className="px-4 py-2.5 font-mono text-slate-700">{p.forecast30}</td>
                <td className="px-4 py-2.5 text-slate-600">{p.daysUntilStockout !== null ? `${p.daysUntilStockout}d` : "—"}</td>
                <td className="px-4 py-2.5 text-slate-600">{usd(p.stockValue)}</td>
                <td className="px-4 py-2.5">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${classStyles[p.classification]}`}>
                    {classLabel[p.classification]}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
