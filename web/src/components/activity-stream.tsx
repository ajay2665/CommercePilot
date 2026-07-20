"use client";

import { Bell, Boxes, MessageSquareText, ShoppingCart } from "lucide-react";
import type { ActivityItem } from "@/lib/api";

const ICONS = {
  order: ShoppingCart,
  ticket: MessageSquareText,
  inventory: Boxes,
  notification: Bell,
} as const;

const SEVERITY_DOT = {
  info: "bg-slate-300",
  warning: "bg-amber-500",
  critical: "bg-red-500",
} as const;

/** Unified feed of recent orders, tickets, inventory alerts and notifications. */
export function ActivityStream({ items }: { items: ActivityItem[] }) {
  if (items.length === 0)
    return <p className="px-5 py-8 text-center text-sm text-slate-400">No recent activity.</p>;

  return (
    <ul className="divide-y divide-slate-50">
      {items.map((item, i) => {
        const Icon = ICONS[item.type] ?? Bell;
        return (
          <li key={`${item.referenceId ?? item.title}-${i}`} className="flex items-center gap-3 px-5 py-2.5 text-sm">
            <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
              <Icon size={13} />
            </span>
            <span className={`h-1.5 w-1.5 shrink-0 rounded-full ${SEVERITY_DOT[item.severity] ?? SEVERITY_DOT.info}`} />
            <span className="min-w-0 flex-1 truncate">
              <span className="font-medium text-slate-700">{item.title}</span>
              {item.description && <span className="ml-2 text-xs text-slate-400">{item.description}</span>}
            </span>
            <span className="shrink-0 text-xs tabular-nums text-slate-400">{relativeTime(item.timestamp)}</span>
          </li>
        );
      })}
    </ul>
  );
}

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60_000);
  if (minutes < 1) return "now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}
