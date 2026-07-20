"use client";

import { PERIODS, usePeriodStore } from "@/features/dashboard/period-store";

/** Global period filter — drives every dashboard widget via the period store. */
export function PeriodSelector() {
  const period = usePeriodStore((s) => s.period);
  const setPeriod = usePeriodStore((s) => s.setPeriod);

  return (
    <div className="inline-flex rounded-lg border border-slate-200 bg-white p-0.5">
      {PERIODS.map((p) => (
        <button
          key={p.key}
          onClick={() => setPeriod(p.key)}
          className={`rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
            period === p.key
              ? "bg-slate-900 text-white"
              : "text-slate-500 hover:bg-slate-100 hover:text-slate-800"
          }`}
        >
          {p.label}
        </button>
      ))}
    </div>
  );
}
