"use client";

import type { LucideIcon } from "lucide-react";

const SERIES = "#2a78d6"; // validated primary series hue

interface KpiCardProps {
  label: string;
  value: string | null;
  change?: number | null; // % vs prior period
  hint?: string | null;
  sparkline?: number[];
  icon: LucideIcon;
  alert?: boolean;
}

/** Stat tile: value, delta vs prior period, optional sparkline. */
export function KpiCard({ label, value, change, hint, sparkline, icon: Icon, alert }: KpiCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5">
      <div className="flex items-center justify-between">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
        <Icon size={15} className="text-slate-300" />
      </div>
      <div className="mt-1 flex items-end justify-between gap-2">
        <p className={`text-3xl font-semibold ${alert ? "text-red-600" : "text-slate-900"}`}>
          {value ?? "—"}
        </p>
        {sparkline && sparkline.length > 1 && <Sparkline data={sparkline} />}
      </div>
      <div className="mt-1.5 flex items-center gap-2 text-xs">
        {change !== undefined && change !== null && (
          <span className={`font-medium ${change >= 0 ? "text-emerald-700" : "text-red-700"}`}>
            {change >= 0 ? "↑" : "↓"} {Math.abs(change).toFixed(1)}%
            <span className="ml-1 font-normal text-slate-400">vs prior</span>
          </span>
        )}
        {hint && <span className="text-slate-400">{hint}</span>}
      </div>
    </div>
  );
}

function Sparkline({ data }: { data: number[] }) {
  const w = 84;
  const h = 28;
  const max = Math.max(...data, 1);
  const min = Math.min(...data, 0);
  const range = max - min || 1;
  const points = data
    .map((v, i) => {
      const x = (i / (data.length - 1)) * (w - 2) + 1;
      const y = h - 2 - ((v - min) / range) * (h - 4);
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");

  return (
    <svg width={w} height={h} className="shrink-0" aria-hidden="true">
      <polyline
        points={points}
        fill="none"
        stroke={SERIES}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
