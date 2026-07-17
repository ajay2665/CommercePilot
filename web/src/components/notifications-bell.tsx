"use client";

import { useState } from "react";
import { Bell, Check } from "lucide-react";
import { useAcknowledge, useNotifications } from "@/features/support/hooks";

const severityDot = {
  info: "bg-blue-500",
  warning: "bg-amber-500",
  critical: "bg-red-600",
} as const;

export function NotificationsBell() {
  const [open, setOpen] = useState(false);
  const { data } = useNotifications();
  const acknowledge = useAcknowledge();

  const unread = data?.filter((n) => !n.acknowledged) ?? [];

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="relative rounded-full border border-slate-200 bg-white p-2 text-slate-600 hover:bg-slate-50"
        aria-label="Notifications"
      >
        <Bell size={16} />
        {unread.length > 0 && (
          <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-bold text-white">
            {unread.length}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 z-50 mt-2 max-h-96 w-96 overflow-auto rounded-xl border border-slate-200 bg-white shadow-lg">
          <div className="border-b border-slate-100 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
            Team notifications
          </div>
          {(data?.length ?? 0) === 0 && (
            <p className="px-4 py-6 text-center text-sm text-slate-400">Nothing yet.</p>
          )}
          {data?.map((n) => (
            <div
              key={n.id}
              className={`flex gap-3 border-b border-slate-50 px-4 py-3 text-sm ${n.acknowledged ? "opacity-50" : ""}`}
            >
              <span className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${severityDot[n.severity]}`} />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-slate-800">{n.title}</p>
                <p className="truncate text-xs text-slate-500">{n.message}</p>
                <p className="mt-0.5 text-[11px] text-slate-400">
                  #{n.channel} · {new Date(n.createdAt).toLocaleTimeString()}
                </p>
              </div>
              {!n.acknowledged && (
                <button
                  onClick={() => acknowledge.mutate(n.id)}
                  className="self-center rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-emerald-600"
                  title="Acknowledge"
                >
                  <Check size={14} />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
