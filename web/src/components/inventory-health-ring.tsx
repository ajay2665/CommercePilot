"use client";

import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import type { HealthSummary } from "@/lib/api";

// Validated 6-slot cycle (order matters for adjacent-segment separation).
const CLASSES = [
  { key: "healthy", label: "Healthy", color: "#008300" },
  { key: "fast", label: "Fast movers", color: "#2a78d6" },
  { key: "overstock", label: "Overstock", color: "#e87ba4" },
  { key: "slow", label: "Slow movers", color: "#eda100" },
  { key: "dead", label: "Dead stock", color: "#4a3aa7" },
  { key: "lowStock", label: "Low stock", color: "#e34948" },
] as const;

/** Stock class distribution ring with the health score in the center. */
export function InventoryHealthRing({ health }: { health: HealthSummary }) {
  const counts: Record<(typeof CLASSES)[number]["key"], number> = {
    healthy: Math.max(
      0,
      health.totalProducts - health.fastCount - health.slowCount - health.deadCount
        - health.overstockCount - health.lowStockCount,
    ),
    fast: health.fastCount,
    overstock: health.overstockCount,
    slow: health.slowCount,
    dead: health.deadCount,
    lowStock: health.lowStockCount,
  };
  const segments = CLASSES.filter((c) => counts[c.key] > 0).map((c) => ({
    name: c.label,
    value: counts[c.key],
    color: c.color,
  }));

  return (
    <div className="flex items-center gap-4">
      <div className="relative h-36 w-36 shrink-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={segments}
              dataKey="value"
              nameKey="name"
              innerRadius="68%"
              outerRadius="100%"
              paddingAngle={2}
              stroke="#ffffff"
              strokeWidth={2}
              startAngle={90}
              endAngle={-270}
            >
              {segments.map((s) => (
                <Cell key={s.name} fill={s.color} />
              ))}
            </Pie>
            <Tooltip content={<RingTooltip total={health.totalProducts} />} />
          </PieChart>
        </ResponsiveContainer>
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
          <p className="text-2xl font-semibold text-slate-900">{health.healthScore}</p>
          <p className="text-[10px] uppercase tracking-wide text-slate-400">Health</p>
        </div>
      </div>
      <ul className="min-w-0 flex-1 space-y-1.5 text-xs">
        {CLASSES.map((c) => (
          <li key={c.key} className="flex items-center gap-2">
            <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ background: c.color }} />
            <span className="truncate text-slate-600">{c.label}</span>
            <span className="ml-auto font-medium tabular-nums text-slate-800">{counts[c.key]}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

interface RingPayload {
  payload?: { name: string; value: number };
}

function RingTooltip({ active, payload, total }: { active?: boolean; payload?: RingPayload[]; total: number }) {
  const seg = payload?.[0]?.payload;
  if (!active || !seg) return null;
  const pct = total > 0 ? ((seg.value / total) * 100).toFixed(0) : "0";
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs shadow-sm">
      <p className="font-medium text-slate-800">{seg.name}</p>
      <p className="text-slate-500">
        {seg.value} products · {pct}%
      </p>
    </div>
  );
}
