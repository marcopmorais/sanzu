const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface TenantPosture {
  tenantId: string;
  tenantName: string;
  location: string;
  status: string;
  subscriptionPlan: string | null;
  activeCases: number;
  blockedTasks: number;
  openKpiAlerts: number;
  failedPaymentAttempts: number;
  createdAt: string;
  onboardingCompletedAt: string | null;
}

export interface FleetPostureResponse {
  totalTenants: number;
  activeTenants: number;
  onboardingTenants: number;
  paymentIssueTenants: number;
  suspendedTenants: number;
  generatedAt: string;
  tenants: TenantPosture[];
}

export interface TenantDrilldownMetrics {
  totalCases: number;
  activeCases: number;
  closedCases: number;
  blockedTasks: number;
  completedTasks: number;
  documentsUploaded: number;
}

export interface ReasonCodeCount {
  reasonCategory: string;
  label: string;
  count: number;
}

export interface TenantDrilldownResponse {
  tenantId: string;
  tenantName: string;
  location: string;
  status: string;
  subscriptionPlan: string | null;
  subscriptionBillingCycle: string | null;
  failedPaymentAttempts: number;
  createdAt: string;
  onboardingCompletedAt: string | null;
  subscriptionActivatedAt: string | null;
  metrics: TenantDrilldownMetrics;
  blockedByReason: ReasonCodeCount[];
}

export async function getFleetPosture(
  search?: string,
  status?: string,
): Promise<FleetPostureResponse> {
  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (status) params.set("status", status);
  const qs = params.toString() ? `?${params.toString()}` : "";
  const res = await fetch(`${API_BASE}/api/v1/admin/fleet${qs}`, {
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`Fleet fetch failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function getTenantDrilldown(
  tenantId: string,
): Promise<TenantDrilldownResponse> {
  const res = await fetch(`${API_BASE}/api/v1/admin/fleet/${tenantId}`, {
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`Drilldown fetch failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
