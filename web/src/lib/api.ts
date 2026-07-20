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

// ── Inventory pod (Phase 2) ──────────────────────────────────────────────────

export type StockClass = "healthy" | "fast" | "slow" | "dead" | "overstock" | "lowStock";

export interface InventoryRow {
  productId: string;
  sku: string;
  name: string;
  brand: string;
  price: number;
  currentStock: number;
  safetyStock: number;
  reorderPoint: number;
  leadTimeDays: number;
  dailyRate: number;
  forecast30: number;
  daysUntilStockout: number | null;
  classification: StockClass;
  stockValue: number;
}

export interface HealthSummary {
  healthScore: number;
  totalProducts: number;
  totalStockValue: number;
  fastCount: number;
  slowCount: number;
  deadCount: number;
  overstockCount: number;
  lowStockCount: number;
  deadValue: number;
  overstockValue: number;
  generatedAt: string;
}

export interface SeriesPoint {
  date: string;
  units: number;
}

export interface ForecastSeries {
  productId: string;
  sku: string;
  name: string;
  brand: string;
  history: SeriesPoint[];
  forecast: SeriesPoint[];
  dailyRate: number;
  confidence: number;
  horizonDays: number;
}

export interface InventoryAlert {
  id: string;
  productId: string;
  type: "lowStock" | "stockoutRisk" | "deadStock" | "overstock";
  severity: "info" | "warning" | "critical";
  message: string;
  acknowledged: boolean;
  createdAt: string;
}

export interface ReorderSuggestion {
  id: string;
  productId: string;
  supplierId: string | null;
  quantity: number;
  orderByDate: string;
  rationale: string;
  generatedAt: string;
}

// ── Shopping pod (Phase 3) ───────────────────────────────────────────────────

export interface ShopProductCard {
  productId: string;
  sku: string;
  name: string;
  brand: string;
  price: number;
  inStock: boolean;
  currentStock: number;
  score: number;
  reason: string;
}

export interface CustomerLite {
  id: string;
  name: string;
  email: string;
}

export interface AssistantAnswer {
  answer: string;
  sources: { title: string; kind: string }[];
}

export const podApi = {
  inventoryProducts: () => request<InventoryRow[]>("/api/inventory/products"),
  inventoryHealth: () => request<HealthSummary>("/api/inventory/health"),
  inventoryForecast: (productId: string, horizon: number) =>
    request<ForecastSeries>(`/api/inventory/forecast?productId=${productId}&horizon=${horizon}`),
  inventoryAlerts: (unacknowledgedOnly: boolean) =>
    request<InventoryAlert[]>(`/api/inventory/alerts?unacknowledgedOnly=${unacknowledgedOnly}`),
  ackInventoryAlert: (id: string) => request<void>(`/api/inventory/alerts/${id}/ack`, { method: "POST" }),
  reorderSuggestions: () => request<ReorderSuggestion[]>("/api/inventory/reorder-suggestions"),
  copilot: (question: string) =>
    request<{ answer: string }>("/api/inventory/copilot", {
      method: "POST",
      body: JSON.stringify({ question }),
    }),

  shoppingSearch: (q: string) =>
    request<ShopProductCard[]>(`/api/shopping/search?q=${encodeURIComponent(q)}`),
  recommendations: (customerId: string) =>
    request<ShopProductCard[]>(`/api/shopping/recommendations?customerId=${customerId}`),
  trending: () => request<ShopProductCard[]>("/api/shopping/trending"),
  customers: () => request<CustomerLite[]>("/api/shopping/customers"),
  assistant: (question: string) =>
    request<AssistantAnswer>("/api/shopping/assistant", {
      method: "POST",
      body: JSON.stringify({ question }),
    }),
  compare: (productIds: string[]) =>
    request<{ comparison: string }>("/api/shopping/compare", {
      method: "POST",
      body: JSON.stringify({ productIds }),
    }),
};

// ── Executive dashboard (Phase 4) ────────────────────────────────────────────

export interface SalesBucket {
  date: string;
  revenue: number;
  orders: number;
}

export interface DashboardSummary {
  revenue: number;
  revenuePrior: number;
  revenueGrowthPct: number;
  orderCount: number;
  orderCountPrior: number;
  orderGrowthPct: number;
  avgOrderValue: number;
  activeCustomers: number;
  inventoryHealthScore: number;
  lowStockCount: number;
  activeTickets: number;
  escalatedTickets: number;
  sparkline: SalesBucket[];
}

export interface SalesTrend {
  granularity: "day" | "week" | "month";
  current: SalesBucket[];
  prior: SalesBucket[];
}

export interface TopCustomer {
  customerId: string;
  name: string;
  email: string;
  totalSpend: number;
  orderCount: number;
}

export interface TopProduct {
  productId: string;
  name: string;
  brand: string;
  sku: string;
  quantitySold: number;
  revenue: number;
}

export interface SupportSnapshot {
  created: number;
  queued: number;
  triaged: number;
  escalated: number;
  resolved: number;
  highUrgency: number;
  avgTriageMinutes: number | null;
}

export interface ActivityItem {
  type: "order" | "ticket" | "inventory" | "notification";
  title: string;
  description: string;
  severity: "info" | "warning" | "critical";
  timestamp: string;
  referenceId: string | null;
}

export interface AiFeatureUsage {
  feature: string;
  calls: number;
  inputTokens: number;
  outputTokens: number;
  costUsd: number;
  avgLatencyMs: number;
  failures: number;
}

export interface AiUsageBucket {
  date: string;
  calls: number;
  costUsd: number;
}

export interface AiUsageStats {
  totalCalls: number;
  failures: number;
  inputTokens: number;
  outputTokens: number;
  costUsd: number;
  avgLatencyMs: number;
  byFeature: AiFeatureUsage[];
  byDay: AiUsageBucket[];
}

export interface DashboardRange {
  from?: string;
  to?: string;
  granularity?: "day" | "week" | "month";
  take?: number;
}

function rangeParams(range: DashboardRange = {}): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(range)) {
    if (value !== undefined && value !== null) params.set(key, String(value));
  }
  return params.toString();
}

export const dashApi = {
  summary: (r?: DashboardRange) =>
    request<DashboardSummary>(`/api/dashboard/summary?${rangeParams(r)}`),
  sales: (r?: DashboardRange) =>
    request<SalesTrend>(`/api/dashboard/sales?${rangeParams(r)}`),
  topCustomers: (r?: DashboardRange) =>
    request<TopCustomer[]>(`/api/dashboard/top-customers?${rangeParams(r)}`),
  topProducts: (r?: DashboardRange) =>
    request<TopProduct[]>(`/api/dashboard/top-products?${rangeParams(r)}`),
  supportSnapshot: (r?: DashboardRange) =>
    request<SupportSnapshot>(`/api/dashboard/support-snapshot?${rangeParams(r)}`),
  recentActivity: (take = 20) =>
    request<ActivityItem[]>(`/api/dashboard/recent-activity?take=${take}`),
  aiUsage: (r?: DashboardRange) =>
    request<AiUsageStats>(`/api/dashboard/ai-usage?${rangeParams(r)}`),
};

// ── AI command center (Phase 4) ──────────────────────────────────────────────

export interface ChatSource {
  title: string;
  kind: string;
}

export interface ChatActionLink {
  label: string;
  route: string;
}

export interface ChatChart {
  type: "bar" | "line" | "area" | "doughnut";
  labels: string[];
  datasets: { label: string; data: number[] }[];
}

export interface BrainChatResponse {
  reply: string;
  conversationId: string;
  intent: string;
  domains: string[];
  sources: ChatSource[];
  actions: ChatActionLink[];
  chart: ChatChart | null;
}

export const brainApi = {
  chat: (body: { message: string; conversationId?: string; currentPage?: string }) =>
    request<BrainChatResponse>("/api/brain/chat", {
      method: "POST",
      body: JSON.stringify(body),
    }),
};
