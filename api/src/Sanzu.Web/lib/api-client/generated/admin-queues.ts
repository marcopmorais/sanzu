const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface AdminQueueSummary {
  queueId: string;
  name: string;
  scope: string;
  itemCount: number;
}

export interface AdminQueueListResponse {
  generatedAt: string;
  queues: AdminQueueSummary[];
}

export interface AdminQueueItem {
  itemId: string;
  tenantId: string;
  tenantName: string;
  reasonCategory: string;
  reasonLabel: string;
  summary: string;
  detectedAt: string;
}

export interface AdminQueueItemsResponse {
  queueId: string;
  queueName: string;
  generatedAt: string;
  items: AdminQueueItem[];
}

export interface AdminEventStreamEntry {
  eventId: string;
  eventType: string;
  reasonCategory: string | null;
  safeSummary: string;
  createdAt: string;
}

export interface AdminEventStreamResponse {
  tenantId: string;
  tenantName: string;
  generatedAt: string;
  events: AdminEventStreamEntry[];
}

export async function listAdminQueues(): Promise<AdminQueueListResponse> {
  const res = await fetch(`${API_BASE}/api/v1/admin/queues`, { cache: "no-store" });
  if (!res.ok) throw new Error(`Queue list failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function getQueueItems(queueId: string): Promise<AdminQueueItemsResponse> {
  const res = await fetch(`${API_BASE}/api/v1/admin/queues/${queueId}`, { cache: "no-store" });
  if (!res.ok) throw new Error(`Queue items failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function getTenantEventStream(
  tenantId: string,
  limit = 50,
): Promise<AdminEventStreamResponse> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/queues/events/${tenantId}?limit=${limit}`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Event stream failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
