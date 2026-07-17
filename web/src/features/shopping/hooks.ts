"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { podApi } from "@/lib/api";

export function useShoppingSearch(query: string) {
  return useQuery({
    queryKey: ["shopping", "search", query],
    queryFn: () => podApi.shoppingSearch(query),
    enabled: query.trim().length > 1,
    staleTime: 60000,
  });
}

export function useTrending() {
  return useQuery({ queryKey: ["shopping", "trending"], queryFn: podApi.trending });
}

export function useRecommendations(customerId: string | null) {
  return useQuery({
    queryKey: ["shopping", "recommendations", customerId],
    queryFn: () => podApi.recommendations(customerId!),
    enabled: Boolean(customerId),
  });
}

export function useCustomers() {
  return useQuery({ queryKey: ["shopping", "customers"], queryFn: podApi.customers });
}

export function useAssistant() {
  return useMutation({ mutationFn: podApi.assistant });
}
