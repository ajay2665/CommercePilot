"use client";

import { useWorkerStatus } from "@/features/support/hooks";

/** Header pill mirroring SupportDeskAI's dashboard: queue depth + current pipeline stage. */
export function StatusPill() {
  const { data } = useWorkerStatus();

  const busy = Boolean(data?.processingTicketId);
  const label = busy
    ? `${data?.stage ?? "Working"}: ${truncate(data?.processingSubject ?? "", 40)}`
    : data && data.queued > 0
      ? `${data.queued} queued`
      : "Idle";

  return (
    <div className="flex items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-600">
      <span
        className={`h-2 w-2 rounded-full ${busy ? "animate-pulse bg-amber-500" : "bg-emerald-500"}`}
      />
      <span className="max-w-64 truncate">{label}</span>
      {data && data.queued > 0 && busy && (
        <span className="rounded-full bg-slate-100 px-1.5 font-medium">+{data.queued}</span>
      )}
    </div>
  );
}

function truncate(text: string, max: number) {
  return text.length > max ? text.slice(0, max) + "…" : text;
}
