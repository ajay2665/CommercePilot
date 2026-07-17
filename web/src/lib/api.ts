// Hand-written v1 types mirroring the FastEndpoints contracts (camelCase JSON,
// enums as camelCase strings). Replaced by OpenAPI codegen (`npm run gen:api`)
// once the contract stabilises.

export type TicketStatus = "queued" | "triaged" | "escalated" | "resolved" | "discarded";
export type TicketUrgency = "low" | "medium" | "high";
export type TicketSentiment = "positive" | "neutral" | "negative";
export type TicketCategory =
  | "refund" | "shipping" | "warranty" | "technical" | "payment" | "complaint" | "other";

export interface Ticket {
  id: string;
  subject: string;
  body: string;
  sender: string;
  brand: string | null;
  category: TicketCategory | null;
  urgency: TicketUrgency | null;
  sentiment: TicketSentiment | null;
  confidence: number | null;
  summary: string | null;
  status: TicketStatus;
  assignedTeam: string | null;
  createdAt: string;
  triagedAt: string | null;
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AppNotification {
  id: string;
  channel: string;
  title: string;
  message: string;
  severity: "info" | "warning" | "critical";
  ticketId: string | null;
  acknowledged: boolean;
  createdAt: string;
}

export interface WorkerStatus {
  queued: number;
  processingTicketId: string | null;
  processingSubject: string | null;
  stage: string | null;
}

export interface TicketFilters {
  status?: TicketStatus | "";
  urgency?: TicketUrgency | "";
  brand?: string;
  team?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  // Only send Content-Type: application/json when there's a body — FastEndpoints
  // treats the header as a promise of a JSON body and fails parsing an empty one.
  const res = await fetch(`${API}${path}`, {
    ...init,
    headers: init?.body ? { "Content-Type": "application/json", ...init?.headers } : init?.headers,
  });
  if (!res.ok) throw new Error(`API ${res.status}: ${res.statusText}`);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  tickets(filters: TicketFilters = {}): Promise<Paged<Ticket>> {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== "" && value !== null) params.set(key, String(value));
    }
    return request(`/api/support/tickets?${params}`);
  },
  ticket: (id: string) => request<Ticket>(`/api/support/tickets/${id}`),
  submitTicket: (body: { subject: string; body: string; sender: string }) =>
    request<{ ticketId: string; status: string }>("/api/support/triage", {
      method: "POST",
      body: JSON.stringify(body),
    }),
  notifications: (take = 25) => request<AppNotification[]>(`/api/notifications?take=${take}`),
  acknowledge: (id: string) => request<void>(`/api/notifications/${id}/ack`, { method: "POST" }),
  workerStatus: () => request<WorkerStatus>("/api/status"),
};

export const BRANDS = ["Aurora Audio", "Peak Outdoors", "Luma Beauty", "Nordic Home", "VoltEdge"];
