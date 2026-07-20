"use client";

import { useMemo } from "react";
import {
  Area,
  Bar,
  BarChart,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { SalesTrend } from "@/lib/api";

const CURRENT = "#2a78d6"; // validated primary series hue
const PRIOR = "#6da7ec"; // lighter ordinal step of the same hue, drawn dashed
const GRID = "#e2e8f0";
const AXIS = "#94a3b8";

/**
 * Revenue & orders trend as two stacked single-axis panels sharing the x axis
 * (never a dual-axis chart). Prior period rides along as a dashed reference.
 */
export function RevenueChart({ trend }: { trend: SalesTrend }) {
  const data = useMemo(
    () =>
      trend.current.map((b, i) => ({
        date: formatBucket(b.date, trend.granularity),
        revenue: b.revenue,
        orders: b.orders,
        prior: trend.prior[i]?.revenue ?? null,
      })),
    [trend],
  );

  const interval = Math.max(0, Math.ceil(data.length / 8) - 1);

  return (
    <div>
      <div className="flex items-center justify-between px-1">
        <p className="text-xs font-medium text-slate-500">Revenue</p>
        <div className="flex items-center gap-3 text-[11px] text-slate-500">
          <span className="flex items-center gap-1.5">
            <span className="h-0.5 w-4 rounded-full" style={{ background: CURRENT }} />
            Current
          </span>
          <span className="flex items-center gap-1.5">
            <svg width="16" height="2" aria-hidden="true">
              <line x1="0" y1="1" x2="16" y2="1" stroke={PRIOR} strokeWidth="2" strokeDasharray="4 3" />
            </svg>
            Prior period
          </span>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={190}>
        <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 0 }} syncId="sales">
          <CartesianGrid vertical={false} stroke={GRID} strokeWidth={1} />
          <XAxis dataKey="date" tick={{ fontSize: 10, fill: AXIS }} tickLine={false} axisLine={false} interval={interval} />
          <YAxis
            tick={{ fontSize: 10, fill: AXIS }}
            tickLine={false}
            axisLine={false}
            width={44}
            tickFormatter={(v: number) => `$${compact(v)}`}
          />
          <Tooltip content={<SalesTooltip />} cursor={{ stroke: "#64748b", strokeWidth: 1, opacity: 0.35 }} />
          <Area type="monotone" dataKey="revenue" stroke={CURRENT} strokeWidth={2} fill={CURRENT} fillOpacity={0.07} />
          <Line type="monotone" dataKey="prior" stroke={PRIOR} strokeWidth={2} strokeDasharray="5 4" dot={false} />
        </ComposedChart>
      </ResponsiveContainer>

      <p className="mt-2 px-1 text-xs font-medium text-slate-500">Orders</p>
      <ResponsiveContainer width="100%" height={96}>
        <BarChart data={data} margin={{ top: 4, right: 8, bottom: 0, left: 0 }} syncId="sales" barCategoryGap="30%">
          <CartesianGrid vertical={false} stroke={GRID} strokeWidth={1} />
          <XAxis dataKey="date" tick={{ fontSize: 10, fill: AXIS }} tickLine={false} axisLine={false} interval={interval} />
          <YAxis tick={{ fontSize: 10, fill: AXIS }} tickLine={false} axisLine={false} width={44} allowDecimals={false} />
          <Tooltip content={<SalesTooltip />} cursor={{ fill: "#64748b", opacity: 0.06 }} />
          <Bar dataKey="orders" fill={CURRENT} radius={[4, 4, 0, 0]} maxBarSize={16} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

interface TooltipEntry {
  dataKey?: string | number;
  value?: number | string;
}

function SalesTooltip({ active, payload, label }: { active?: boolean; payload?: TooltipEntry[]; label?: string }) {
  if (!active || !payload?.length) return null;
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs shadow-sm">
      <p className="font-medium text-slate-800">{label}</p>
      {payload.map((entry) => (
        <p key={String(entry.dataKey)} className="text-slate-500">
          {entry.dataKey === "revenue" && `Revenue $${Number(entry.value).toFixed(2)}`}
          {entry.dataKey === "prior" && `Prior period $${Number(entry.value).toFixed(2)}`}
          {entry.dataKey === "orders" && `${entry.value} orders`}
        </p>
      ))}
    </div>
  );
}

function compact(v: number): string {
  if (Math.abs(v) >= 1000) return `${(v / 1000).toFixed(1)}k`;
  return v.toFixed(0);
}

function formatBucket(date: string, granularity: SalesTrend["granularity"]): string {
  const d = new Date(`${date}T00:00:00`);
  if (granularity === "month") return d.toLocaleDateString(undefined, { month: "short" });
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric" });
}
