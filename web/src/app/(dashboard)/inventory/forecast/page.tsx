"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { ForecastChart } from "@/components/forecast-chart";
import { useForecast, useInventoryProducts } from "@/features/inventory/hooks";

function ForecastView() {
  const params = useSearchParams();
  const { data: products } = useInventoryProducts();
  const [productId, setProductId] = useState<string | null>(params.get("productId"));
  const [horizon, setHorizon] = useState(30);

  useEffect(() => {
    if (!productId && products?.length) setProductId(products[0].productId);
  }, [products, productId]);

  const { data: series, isLoading } = useForecast(productId, horizon);

  const stats = series
    ? [
        { label: "Daily rate (weighted avg)", value: `${series.dailyRate.toFixed(2)} /day` },
        { label: `${horizon}d forecast`, value: `${Math.round(series.dailyRate * horizon)} units` },
        { label: "Confidence", value: `${Math.round(series.confidence * 100)}%` },
        {
          label: "Current stock",
          value: products?.find((p) => p.productId === productId)?.currentStock ?? "—",
        },
      ]
    : [];

  return (
    <div className="space-y-4">
      <Link href="/inventory" className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-800">
        <ArrowLeft size={14} /> Inventory health
      </Link>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">Demand forecast</h1>
          <p className="text-sm text-slate-500">60-day sales history (7-day average) and constant-rate projection.</p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={productId ?? ""}
            onChange={(e) => setProductId(e.target.value)}
            className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-slate-400"
          >
            {products?.map((p) => (
              <option key={p.productId} value={p.productId}>
                {p.brand} — {p.name}
              </option>
            ))}
          </select>
          <div className="flex overflow-hidden rounded-lg border border-slate-200 bg-white text-sm">
            {[7, 30, 90].map((h) => (
              <button
                key={h}
                onClick={() => setHorizon(h)}
                className={`px-3 py-2 ${horizon === h ? "bg-slate-900 text-white" : "text-slate-600 hover:bg-slate-50"}`}
              >
                {h}d
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map((s) => (
          <div key={s.label} className="rounded-2xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{s.label}</p>
            <p className="mt-1 text-2xl font-semibold text-slate-900">{s.value}</p>
          </div>
        ))}
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-5">
        {isLoading && <p className="py-16 text-center text-sm text-slate-400">Loading series…</p>}
        {series && (
          <>
            <p className="mb-3 text-sm font-semibold text-slate-700">
              {series.brand} — {series.name} <span className="ml-1 font-normal text-slate-400">({series.sku}) · units/day</span>
            </p>
            <ForecastChart series={series} />
          </>
        )}
      </div>
    </div>
  );
}

export default function ForecastPage() {
  return (
    <Suspense fallback={<p className="text-sm text-slate-400">Loading…</p>}>
      <ForecastView />
    </Suspense>
  );
}
