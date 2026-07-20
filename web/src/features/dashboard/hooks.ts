"use client";

import { useQuery } from "@tanstack/react-query";
import { dashApi } from "@/lib/api";
import { periodRange, usePeriodStore } from "./period-store";

export function useDashboardSummary() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "summary", period],
    queryFn: () => dashApi.summary(periodRange(period)),
    refetchInterval: 30000,
  });
}

export function useSalesTrend() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "sales", period],
    queryFn: () => dashApi.sales(periodRange(period)),
    refetchInterval: 60000,
  });
}

export function useTopCustomers() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "top-customers", period],
    queryFn: () => dashApi.topCustomers(periodRange(period, 8)),
    refetchInterval: 60000,
  });
}

export function useTopProducts() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "top-products", period],
    queryFn: () => dashApi.topProducts(periodRange(period, 8)),
    refetchInterval: 60000,
  });
}

export function useSupportSnapshot() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "support-snapshot", period],
    queryFn: () => dashApi.supportSnapshot(periodRange(period)),
    refetchInterval: 30000,
  });
}

export function useRecentActivity() {
  return useQuery({
    queryKey: ["dashboard", "recent-activity"],
    queryFn: () => dashApi.recentActivity(20),
    refetchInterval: 15000,
  });
}

export function useAiUsage() {
  const period = usePeriodStore((s) => s.period);
  return useQuery({
    queryKey: ["dashboard", "ai-usage", period],
    queryFn: () => dashApi.aiUsage(periodRange(period)),
    refetchInterval: 60000,
  });
}
