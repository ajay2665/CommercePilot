"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type TicketFilters } from "@/lib/api";

export function useTickets(filters: TicketFilters) {
  return useQuery({
    queryKey: ["support", "tickets", filters],
    queryFn: () => api.tickets(filters),
    refetchInterval: 5000, // polling first; SSE arrives with chat in Phase 1b
    placeholderData: (prev) => prev,
  });
}

export function useTicket(id: string) {
  return useQuery({
    queryKey: ["support", "ticket", id],
    queryFn: () => api.ticket(id),
    refetchInterval: (query) => (query.state.data?.status === "queued" ? 3000 : false),
  });
}

export function useSubmitTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: api.submitTicket,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["support", "tickets"] }),
  });
}

export function useWorkerStatus() {
  return useQuery({
    queryKey: ["platform", "status"],
    queryFn: api.workerStatus,
    refetchInterval: 3000,
  });
}

export function useNotifications() {
  return useQuery({
    queryKey: ["platform", "notifications"],
    queryFn: () => api.notifications(),
    refetchInterval: 5000,
  });
}

export function useAcknowledge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: api.acknowledge,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["platform", "notifications"] }),
  });
}
