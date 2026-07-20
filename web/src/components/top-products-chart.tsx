"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { TopProduct } from "@/lib/api";

const SERIES = "#2a78d6"; // validated primary series hue
const GRID = "#e2e8f0";

/** Horizontal bars, one measure (units sold), direct value labels at bar ends. */
export function TopProductsChart({ products }: { products: TopProduct[] }) {
  const data = products.slice(0, 6).map((p) => ({
    name: p.name.length > 22 ? `${p.name.slice(0, 21)}…` : p.name,
    fullName: p.name,
    brand: p.brand,
    units: p.quantitySold,
    revenue: p.revenue,
  }));

  return (
    <ResponsiveContainer width="100%" height={Math.max(120, data.length * 38)}>
      <BarChart data={data} layout="vertical" margin={{ top: 0, right: 34, bottom: 0, left: 0 }} barCategoryGap="28%">
        <CartesianGrid horizontal={false} stroke={GRID} strokeWidth={1} />
        <XAxis type="number" hide />
        <YAxis
          type="category"
          dataKey="name"
          width={150}
          tick={{ fontSize: 11, fill: "#52514e" }}
          tickLine={false}
          axisLine={false}
        />
        <Tooltip content={<ProductTooltip />} cursor={{ fill: "#64748b", opacity: 0.06 }} />
        <Bar dataKey="units" fill={SERIES} radius={[0, 4, 4, 0]} maxBarSize={14}>
          <LabelList dataKey="units" position="right" style={{ fontSize: 10, fill: "#52514e" }} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

interface ProductPayload {
  payload?: { fullName: string; brand: string; units: number; revenue: number };
}

function ProductTooltip({ active, payload }: { active?: boolean; payload?: ProductPayload[] }) {
  const row = payload?.[0]?.payload;
  if (!active || !row) return null;
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs shadow-sm">
      <p className="font-medium text-slate-800">{row.fullName}</p>
      <p className="text-slate-500">
        {row.brand} · {row.units} units · ${row.revenue.toFixed(2)}
      </p>
    </div>
  );
}
