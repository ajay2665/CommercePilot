"use client";

import { use } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { ConfidenceBadge, SentimentBadge, StatusBadge, UrgencyBadge } from "@/components/badges";
import { useTicket } from "@/features/support/hooks";

export default function TicketDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data: ticket, isLoading } = useTicket(id);

  if (isLoading) return <p className="text-sm text-slate-400">Loading…</p>;
  if (!ticket) return <p className="text-sm text-slate-400">Ticket not found.</p>;

  const fields = [
    { label: "Brand", value: ticket.brand ?? "—" },
    { label: "Category", value: ticket.category ?? "—", capitalize: true },
    { label: "Assigned team", value: ticket.assignedTeam ?? "—" },
    {
      label: "Triaged at",
      value: ticket.triagedAt ? new Date(ticket.triagedAt).toLocaleString() : "pending…",
    },
  ];

  return (
    <div className="max-w-3xl space-y-4">
      <Link
        href="/support"
        className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-800"
      >
        <ArrowLeft size={14} /> Back to tickets
      </Link>

      <div className="rounded-2xl border border-slate-200 bg-white p-6">
        <div className="mb-1 flex items-center gap-2">
          <StatusBadge status={ticket.status} />
          <UrgencyBadge urgency={ticket.urgency} />
          <SentimentBadge sentiment={ticket.sentiment} />
          <span className="ml-auto text-xs text-slate-400">
            {new Date(ticket.createdAt).toLocaleString()}
          </span>
        </div>
        <h1 className="text-lg font-semibold text-slate-900">{ticket.subject}</h1>
        <p className="text-sm text-slate-500">{ticket.sender}</p>

        {ticket.summary && (
          <div className="mt-4 rounded-xl bg-slate-50 p-4 text-sm">
            <p className="mb-1 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
              AI summary <ConfidenceBadge value={ticket.confidence} />
            </p>
            <p className="text-slate-700">{ticket.summary}</p>
          </div>
        )}

        <dl className="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
          {fields.map((f) => (
            <div key={f.label} className="rounded-xl border border-slate-100 p-3">
              <dt className="text-[11px] font-medium uppercase tracking-wide text-slate-400">
                {f.label}
              </dt>
              <dd className={`mt-0.5 text-sm text-slate-800 ${f.capitalize ? "capitalize" : ""}`}>
                {f.value}
              </dd>
            </div>
          ))}
        </dl>

        <div className="mt-5 border-t border-slate-100 pt-4">
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
            Customer message
          </p>
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-slate-700">
            {ticket.body}
          </p>
        </div>
      </div>
    </div>
  );
}
