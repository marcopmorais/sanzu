const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface TrustTelemetryMetrics {
  casesCreated: number;
  casesClosed: number;
  tasksBlocked: number;
  tasksCompleted: number;
  playbooksApplied: number;
  documentsUploaded: number;
}

export interface ReasonCodeCount {
  reasonCategory: string;
  label: string;
  count: number;
}

export interface TelemetryEventSummary {
  eventType: string;
  count: number;
}

export interface TrustTelemetryResponse {
  tenantId: string | null;
  periodDays: number;
  periodStart: string;
  periodEnd: string;
  generatedAt: string;
  metrics: TrustTelemetryMetrics;
  blockedByReason: ReasonCodeCount[];
  eventSummary: TelemetryEventSummary[];
}

export async function getTenantTelemetry(
  tenantId: string,
  periodDays = 30,
): Promise<TrustTelemetryResponse> {
  const res = await fetch(
    `${API_BASE}/api/v1/tenants/${tenantId}/telemetry?periodDays=${periodDays}`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Telemetry fetch failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function getPlatformTelemetry(
  periodDays = 30,
): Promise<TrustTelemetryResponse> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/telemetry?periodDays=${periodDays}`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Platform telemetry fetch failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
