"use client";

import Link from "next/link";
import { useNotifications, useTickets } from "@/features/support/hooks";

export default function DashboardPage() {
  const all = useTickets({ pageSize: 1 });
  const queued = useTickets({ status: "queued", pageSize: 1 });
  const triaged = useTickets({ status: "triaged", pageSize: 1 });
  const escalated = useTickets({ status: "escalated", pageSize: 1 });
  const { data: notifications } = useNotifications();

  const stats = [
    { label: "Total tickets", value: all.data?.total },
    { label: "Queued", value: queued.data?.total },
    { label: "Triaged", value: triaged.data?.total },
    { label: "Escalated", value: escalated.data?.total, alert: true },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold">Executive dashboard</h1>
        <p className="text-sm text-slate-500">
          Support AI is live — Inventory and Shopping pods land in Phases 2–3.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map((s) => (
          <div key={s.label} className="rounded-2xl border border-slate-200 bg-white p-5">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{s.label}</p>
            <p
              className={`mt-1 text-3xl font-semibold ${s.alert && (s.value ?? 0) > 0 ? "text-red-600" : "text-slate-900"}`}
            >
              {s.value ?? "—"}
            </p>
          </div>
        ))}
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-3">
          <h2 className="text-sm font-semibold">Latest team notifications</h2>
          <Link href="/support" className="text-xs text-slate-500 hover:text-slate-800">
            Open Support AI →
          </Link>
        </div>
        {(notifications?.length ?? 0) === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-slate-400">
            No notifications yet — submit a ticket from the Support page.
          </p>
        ) : (
          <ul className="divide-y divide-slate-50">
            {notifications?.slice(0, 6).map((n) => (
              <li key={n.id} className="flex items-center gap-3 px-5 py-3 text-sm">
                <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] font-medium text-slate-500">
                  #{n.channel}
                </span>
                <span className="truncate text-slate-700">{n.title}</span>
                <span className="ml-auto shrink-0 text-xs text-slate-400">
                  {new Date(n.createdAt).toLocaleTimeString()}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
