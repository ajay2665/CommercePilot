"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { podApi } from "@/lib/api";

export function useInventoryProducts() {
  return useQuery({
    queryKey: ["inventory", "products"],
    queryFn: podApi.inventoryProducts,
    refetchInterval: 30000,
  });
}

export function useInventoryHealth() {
  return useQuery({
    queryKey: ["inventory", "health"],
    queryFn: podApi.inventoryHealth,
    refetchInterval: 30000,
  });
}

export function useForecast(productId: string | null, horizon: number) {
  return useQuery({
    queryKey: ["inventory", "forecast", productId, horizon],
    queryFn: () => podApi.inventoryForecast(productId!, horizon),
    enabled: Boolean(productId),
  });
}

export function useInventoryAlerts() {
  return useQuery({
    queryKey: ["inventory", "alerts"],
    queryFn: () => podApi.inventoryAlerts(true),
    refetchInterval: 15000,
  });
}

export function useAckInventoryAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: podApi.ackInventoryAlert,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["inventory", "alerts"] }),
  });
}

export function useReorderSuggestions() {
  return useQuery({
    queryKey: ["inventory", "reorders"],
    queryFn: podApi.reorderSuggestions,
    refetchInterval: 60000,
  });
}

export function useCopilot() {
  return useMutation({ mutationFn: podApi.copilot });
}
