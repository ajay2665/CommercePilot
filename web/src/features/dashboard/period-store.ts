import { create } from "zustand";
import type { DashboardRange } from "@/lib/api";

export type PeriodKey = "1d" | "7d" | "30d" | "90d" | "365d";

export const PERIODS: { key: PeriodKey; label: string; days: number }[] = [
  { key: "1d", label: "Today", days: 1 },
  { key: "7d", label: "7 days", days: 7 },
  { key: "30d", label: "30 days", days: 30 },
  { key: "90d", label: "90 days", days: 90 },
  { key: "365d", label: "1 year", days: 365 },
];

interface PeriodState {
  period: PeriodKey;
  setPeriod: (period: PeriodKey) => void;
}

export const usePeriodStore = create<PeriodState>((set) => ({
  period: "30d",
  setPeriod: (period) => set({ period }),
}));

/** Resolves a period key to API query args at call time (not render time). */
export function periodRange(period: PeriodKey, take?: number): DashboardRange {
  const days = PERIODS.find((p) => p.key === period)?.days ?? 30;
  const to = new Date();
  const from = new Date(to.getTime() - days * 86_400_000);
  return {
    from: from.toISOString(),
    to: to.toISOString(),
    granularity: days > 120 ? "month" : days > 60 ? "week" : "day",
    take,
  };
}
