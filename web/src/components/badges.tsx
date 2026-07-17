import type { TicketSentiment, TicketStatus, TicketUrgency } from "@/lib/api";

const badge = "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium capitalize";

export function StatusBadge({ status }: { status: TicketStatus }) {
  const styles: Record<TicketStatus, string> = {
    queued: "bg-slate-200 text-slate-700",
    triaged: "bg-emerald-100 text-emerald-700",
    escalated: "bg-red-100 text-red-700",
    resolved: "bg-blue-100 text-blue-700",
    discarded: "bg-slate-100 text-slate-400",
  };
  return <span className={`${badge} ${styles[status]}`}>{status}</span>;
}

export function UrgencyBadge({ urgency }: { urgency: TicketUrgency | null }) {
  if (!urgency) return <span className="text-slate-300">—</span>;
  const styles: Record<TicketUrgency, string> = {
    low: "bg-slate-100 text-slate-600",
    medium: "bg-amber-100 text-amber-700",
    high: "bg-red-100 text-red-700",
  };
  return <span className={`${badge} ${styles[urgency]}`}>{urgency}</span>;
}

export function SentimentBadge({ sentiment }: { sentiment: TicketSentiment | null }) {
  if (!sentiment) return <span className="text-slate-300">—</span>;
  const styles: Record<TicketSentiment, string> = {
    positive: "bg-emerald-100 text-emerald-700",
    neutral: "bg-slate-100 text-slate-600",
    negative: "bg-orange-100 text-orange-700",
  };
  return <span className={`${badge} ${styles[sentiment]}`}>{sentiment}</span>;
}

export function ConfidenceBadge({ value }: { value: number | null }) {
  if (value === null) return <span className="text-slate-300">—</span>;
  const tone =
    value >= 0.75 ? "text-emerald-700" : value >= 0.5 ? "text-amber-700" : "text-red-700";
  return <span className={`text-xs font-mono ${tone}`}>{(value * 100).toFixed(0)}%</span>;
}
