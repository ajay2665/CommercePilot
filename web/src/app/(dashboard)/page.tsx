"use client";

import {
  Activity,
  Boxes,
  Cpu,
  DollarSign,
  Flame,
  MessageSquareText,
  ShoppingCart,
  Users,
} from "lucide-react";
import Link from "next/link";
import { ActivityStream } from "@/components/activity-stream";
import { InventoryHealthRing } from "@/components/inventory-health-ring";
import { KpiCard } from "@/components/kpi-card";
import { PeriodSelector } from "@/components/period-selector";
import { RevenueChart } from "@/components/revenue-chart";
import { TopProductsChart } from "@/components/top-products-chart";
import {
  useAiUsage,
  useDashboardSummary,
  useRecentActivity,
  useSalesTrend,
  useSupportSnapshot,
  useTopCustomers,
  useTopProducts,
} from "@/features/dashboard/hooks";
import { useInventoryHealth } from "@/features/inventory/hooks";
import { useQuery } from "@tanstack/react-query";
import { podApi, type SupportSnapshot, type TopCustomer } from "@/lib/api";

export default function DashboardPage() {
  const summary = useDashboardSummary();
  const trend = useSalesTrend();
  const topProducts = useTopProducts();
  const topCustomers = useTopCustomers();
  const health = useInventoryHealth();
  const support = useSupportSnapshot();
  const aiUsage = useAiUsage();
  const activity = useRecentActivity();
  const trending = useQuery({
    queryKey: ["shopping", "trending"],
    queryFn: podApi.trending,
    refetchInterval: 60000,
  });

  const s = summary.data;

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">Executive dashboard</h1>
          <p className="text-sm text-slate-500">
            Live view across sales, inventory, support and shopping.
          </p>
        </div>
        <PeriodSelector />
      </div>

      {/* Zone A — KPI cards */}
      <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
        <KpiCard
          label="Revenue"
          icon={DollarSign}
          value={s ? `$${s.revenue.toLocaleString(undefined, { maximumFractionDigits: 0 })}` : null}
          change={s?.revenueGrowthPct}
          sparkline={s?.sparkline.map((b) => b.revenue)}
        />
        <KpiCard
          label="Orders"
          icon={ShoppingCart}
          value={s ? s.orderCount.toLocaleString() : null}
          change={s?.orderGrowthPct}
          hint={s ? `AOV $${s.avgOrderValue.toFixed(2)}` : null}
          sparkline={s?.sparkline.map((b) => b.orders)}
        />
        <KpiCard
          label="Inventory health"
          icon={Boxes}
          value={s ? `${s.inventoryHealthScore}/100` : null}
          hint={s ? `${s.lowStockCount} low stock` : null}
          alert={(s?.lowStockCount ?? 0) > 0 && (s?.inventoryHealthScore ?? 100) < 50}
        />
        <KpiCard
          label="Active tickets"
          icon={MessageSquareText}
          value={s ? s.activeTickets.toLocaleString() : null}
          hint={s ? `${s.escalatedTickets} escalated` : null}
          alert={(s?.escalatedTickets ?? 0) > 0}
        />
      </div>

      {/* Zone B — sales trend + top customers */}
      <div className="grid gap-4 xl:grid-cols-3">
        <Card title="Revenue & orders" className="xl:col-span-2">
          {trend.data ? (
            <RevenueChart trend={trend.data} />
          ) : (
            <Skeleton className="h-[300px]" />
          )}
        </Card>
        <Card title="Top customers" icon={<Users size={14} className="text-slate-300" />}>
          {topCustomers.data ? (
            <TopCustomersList customers={topCustomers.data} />
          ) : (
            <Skeleton className="h-[300px]" />
          )}
        </Card>
      </div>

      {/* Zone C — business intelligence grid */}
      <div className="grid gap-4 xl:grid-cols-3">
        <Card title="Top products by units">
          {topProducts.data ? (
            topProducts.data.length > 0 ? (
              <TopProductsChart products={topProducts.data} />
            ) : (
              <Empty text="No sales in this period." />
            )
          ) : (
            <Skeleton className="h-48" />
          )}
        </Card>
        <Card title="Inventory health">
          {health.data ? <InventoryHealthRing health={health.data} /> : <Skeleton className="h-48" />}
          <Link href="/inventory" className="mt-3 inline-block text-xs text-slate-500 hover:text-slate-800">
            Open Inventory AI →
          </Link>
        </Card>
        <Card title="Support overview">
          {support.data ? (
            <SupportSnapshotCard snapshot={support.data} />
          ) : (
            <Skeleton className="h-48" />
          )}
        </Card>
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        <Card title="Trending products" icon={<Flame size={14} className="text-slate-300" />} className="xl:col-span-2">
          {trending.data ? (
            trending.data.length > 0 ? (
              <div className="flex gap-3 overflow-x-auto pb-1">
                {trending.data.slice(0, 8).map((p) => (
                  <Link
                    key={p.productId}
                    href="/shopping"
                    className="min-w-44 shrink-0 rounded-xl border border-slate-100 bg-slate-50/60 px-3.5 py-3 hover:border-slate-200"
                  >
                    <p className="truncate text-sm font-medium text-slate-800">{p.name}</p>
                    <p className="truncate text-xs text-slate-400">{p.brand}</p>
                    <p className="mt-1.5 text-sm font-semibold text-slate-900">${p.price.toFixed(2)}</p>
                    <p className="mt-0.5 truncate text-[11px] text-slate-400">{p.reason}</p>
                  </Link>
                ))}
              </div>
            ) : (
              <Empty text="No trending signals yet." />
            )
          ) : (
            <Skeleton className="h-28" />
          )}
        </Card>
        <Card title="AI usage" icon={<Cpu size={14} className="text-slate-300" />}>
          {aiUsage.data ? <AiUsageCard usage={aiUsage.data} /> : <Skeleton className="h-28" />}
        </Card>
      </div>

      {/* Zone D — activity stream */}
      <div className="rounded-2xl border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-3">
          <h2 className="flex items-center gap-2 text-sm font-semibold">
            <Activity size={14} className="text-slate-300" />
            Recent activity
          </h2>
        </div>
        {activity.data ? <ActivityStream items={activity.data} /> : <Skeleton className="m-5 h-40" />}
      </div>
    </div>
  );
}

function Card({
  title,
  icon,
  className = "",
  children,
}: {
  title: string;
  icon?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={`rounded-2xl border border-slate-200 bg-white p-5 ${className}`}>
      <h2 className="mb-3 flex items-center justify-between text-sm font-semibold">
        {title}
        {icon}
      </h2>
      {children}
    </div>
  );
}

function Skeleton({ className = "" }: { className?: string }) {
  return <div className={`animate-pulse rounded-xl bg-slate-100 ${className}`} />;
}

function Empty({ text }: { text: string }) {
  return <p className="py-8 text-center text-sm text-slate-400">{text}</p>;
}

function TopCustomersList({ customers }: { customers: TopCustomer[] }) {
  if (customers.length === 0) return <Empty text="No orders in this period." />;
  const max = Math.max(...customers.map((c) => c.totalSpend), 1);
  return (
    <ul className="space-y-2.5">
      {customers.map((c, i) => (
        <li key={c.customerId} className="flex items-center gap-3">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-slate-100 text-[11px] font-semibold text-slate-500">
            {initials(c.name)}
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline justify-between gap-2">
              <p className="truncate text-sm font-medium text-slate-700">
                <span className="mr-1.5 text-xs tabular-nums text-slate-300">{i + 1}.</span>
                {c.name}
              </p>
              <p className="shrink-0 text-sm font-semibold tabular-nums text-slate-900">
                ${c.totalSpend.toLocaleString(undefined, { maximumFractionDigits: 0 })}
              </p>
            </div>
            <div className="mt-1 flex items-center gap-2">
              <div className="h-1 flex-1 overflow-hidden rounded-full bg-slate-100">
                <div
                  className="h-full rounded-full"
                  style={{ width: `${(c.totalSpend / max) * 100}%`, background: "#2a78d6" }}
                />
              </div>
              <span className="shrink-0 text-[11px] text-slate-400">{c.orderCount} orders</span>
            </div>
          </div>
        </li>
      ))}
    </ul>
  );
}

function SupportSnapshotCard({ snapshot }: { snapshot: SupportSnapshot }) {
  const rows = [
    { label: "Created", value: snapshot.created, dot: "bg-slate-300" },
    { label: "Queued", value: snapshot.queued, dot: "bg-slate-400" },
    { label: "Triaged", value: snapshot.triaged, dot: "bg-emerald-500" },
    { label: "Escalated", value: snapshot.escalated, dot: "bg-red-500" },
    { label: "Resolved", value: snapshot.resolved, dot: "bg-blue-500" },
    { label: "High urgency", value: snapshot.highUrgency, dot: "bg-amber-500" },
  ];
  return (
    <div>
      <ul className="space-y-2 text-sm">
        {rows.map((r) => (
          <li key={r.label} className="flex items-center gap-2.5">
            <span className={`h-2 w-2 rounded-full ${r.dot}`} />
            <span className="text-slate-600">{r.label}</span>
            <span className="ml-auto font-medium tabular-nums text-slate-900">{r.value}</span>
          </li>
        ))}
      </ul>
      <p className="mt-3 border-t border-slate-100 pt-2.5 text-xs text-slate-400">
        {snapshot.avgTriageMinutes !== null
          ? `Avg triage time ${snapshot.avgTriageMinutes.toFixed(1)} min`
          : "No triaged tickets in this period."}
        <Link href="/support" className="ml-2 text-slate-500 hover:text-slate-800">
          Open Support AI →
        </Link>
      </p>
    </div>
  );
}

function AiUsageCard({ usage }: { usage: import("@/lib/api").AiUsageStats }) {
  return (
    <div>
      <div className="grid grid-cols-2 gap-3">
        <Stat label="LLM calls" value={usage.totalCalls.toLocaleString()} />
        <Stat label="Cost" value={`$${usage.costUsd.toFixed(4)}`} />
        <Stat label="Avg latency" value={`${(usage.avgLatencyMs / 1000).toFixed(1)}s`} />
        <Stat label="Failures" value={usage.failures.toLocaleString()} alert={usage.failures > 0} />
      </div>
      {usage.byFeature.length > 0 && (
        <ul className="mt-3 space-y-1 border-t border-slate-100 pt-2.5 text-xs">
          {usage.byFeature.slice(0, 4).map((f) => (
            <li key={f.feature} className="flex items-center justify-between">
              <span className="truncate text-slate-500">{f.feature}</span>
              <span className="ml-2 shrink-0 tabular-nums text-slate-700">{f.calls} calls</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function Stat({ label, value, alert }: { label: string; value: string; alert?: boolean }) {
  return (
    <div>
      <p className="text-[11px] uppercase tracking-wide text-slate-400">{label}</p>
      <p className={`text-lg font-semibold ${alert ? "text-red-600" : "text-slate-900"}`}>{value}</p>
    </div>
  );
}

function initials(name: string): string {
  return name
    .split(" ")
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join("")
    .toUpperCase();
}
