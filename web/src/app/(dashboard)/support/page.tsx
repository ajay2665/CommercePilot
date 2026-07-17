"use client";

import { useState } from "react";
import Link from "next/link";
import { Plus, Search } from "lucide-react";
import { ConfidenceBadge, SentimentBadge, StatusBadge, UrgencyBadge } from "@/components/badges";
import { useTickets } from "@/features/support/hooks";
import { SubmitTicketDialog } from "@/features/support/submit-ticket-dialog";
import { BRANDS, type TicketStatus, type TicketUrgency } from "@/lib/api";

export default function SupportPage() {
  const [status, setStatus] = useState<TicketStatus | "">("");
  const [urgency, setUrgency] = useState<TicketUrgency | "">("");
  const [brand, setBrand] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [dialogOpen, setDialogOpen] = useState(false);

  const { data, isLoading } = useTickets({ status, urgency, brand, search, page, pageSize: 15 });
  const totalPages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  const select =
    "rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none focus:border-slate-400";

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Support AI — Tickets</h1>
          <p className="text-sm text-slate-500">
            Intake → LLM triage → routing → team notification, all local.
          </p>
        </div>
        <button
          onClick={() => setDialogOpen(true)}
          className="flex items-center gap-2 rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700"
        >
          <Plus size={16} /> Submit ticket
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search subject or sender…"
            className={`${select} w-64 pl-8`}
          />
        </div>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as TicketStatus | "");
            setPage(1);
          }}
          className={select}
        >
          <option value="">All statuses</option>
          {["queued", "triaged", "escalated", "resolved", "discarded"].map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <select
          value={urgency}
          onChange={(e) => {
            setUrgency(e.target.value as TicketUrgency | "");
            setPage(1);
          }}
          className={select}
        >
          <option value="">All urgencies</option>
          {["low", "medium", "high"].map((u) => (
            <option key={u} value={u}>
              {u}
            </option>
          ))}
        </select>
        <select
          value={brand}
          onChange={(e) => {
            setBrand(e.target.value);
            setPage(1);
          }}
          className={select}
        >
          <option value="">All brands</option>
          {[...BRANDS, "Unknown"].map((b) => (
            <option key={b} value={b}>
              {b}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs uppercase tracking-wide text-slate-400">
              <th className="px-4 py-3 font-medium">Created</th>
              <th className="px-4 py-3 font-medium">Subject</th>
              <th className="px-4 py-3 font-medium">Brand</th>
              <th className="px-4 py-3 font-medium">Category</th>
              <th className="px-4 py-3 font-medium">Urgency</th>
              <th className="px-4 py-3 font-medium">Sentiment</th>
              <th className="px-4 py-3 font-medium">Conf.</th>
              <th className="px-4 py-3 font-medium">Team</th>
              <th className="px-4 py-3 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-50">
            {isLoading && (
              <tr>
                <td colSpan={9} className="px-4 py-10 text-center text-slate-400">
                  Loading…
                </td>
              </tr>
            )}
            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={9} className="px-4 py-10 text-center text-slate-400">
                  No tickets match — submit one to watch the pipeline run.
                </td>
              </tr>
            )}
            {data?.items.map((t) => (
              <tr key={t.id} className="hover:bg-slate-50/60">
                <td className="whitespace-nowrap px-4 py-3 text-xs text-slate-500">
                  {new Date(t.createdAt).toLocaleString()}
                </td>
                <td className="max-w-72 px-4 py-3">
                  <Link
                    href={`/support/${t.id}`}
                    className="block truncate font-medium text-slate-800 hover:underline"
                  >
                    {t.subject}
                  </Link>
                  <span className="block truncate text-xs text-slate-400">{t.sender}</span>
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-slate-600">{t.brand ?? "—"}</td>
                <td className="px-4 py-3 capitalize text-slate-600">{t.category ?? "—"}</td>
                <td className="px-4 py-3">
                  <UrgencyBadge urgency={t.urgency} />
                </td>
                <td className="px-4 py-3">
                  <SentimentBadge sentiment={t.sentiment} />
                </td>
                <td className="px-4 py-3">
                  <ConfidenceBadge value={t.confidence} />
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                  {t.assignedTeam ?? "—"}
                </td>
                <td className="px-4 py-3">
                  <StatusBadge status={t.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-slate-500">
        <span>
          {data?.total ?? 0} ticket{(data?.total ?? 0) === 1 ? "" : "s"}
        </span>
        <div className="flex items-center gap-2">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40"
          >
            Prev
          </button>
          <span>
            {page} / {totalPages}
          </span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      </div>

      <SubmitTicketDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
    </div>
  );
}
