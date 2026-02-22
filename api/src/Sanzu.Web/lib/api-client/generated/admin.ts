/**
 * Admin API Client
 * Auto-generated - do not manually edit
 */

export interface AdminPermissionsResponse {
  role: string;
  accessibleEndpoints: string[];
  accessibleWidgets: string[];
  canTakeActions: boolean;
}

export async function getAdminPermissions(): Promise<AdminPermissionsResponse> {
  const response = await fetch('/api/v1/admin/me/permissions', {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to get admin permissions: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export interface PlatformOperationsSummaryResponse {
  totalActiveTenants: number;
  totalActiveCases: number;
  workflowStepsCompleted: number;
  workflowStepsActive: number;
  workflowStepsBlocked: number;
  totalDocuments: number;
}

export async function getPlatformSummary(): Promise<PlatformOperationsSummaryResponse> {
  const response = await fetch('/api/v1/admin/platform/summary', {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to get platform summary: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export interface AdminTeamMemberResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  grantedAt: string;
}

export async function listTeamMembers(): Promise<AdminTeamMemberResponse[]> {
  const response = await fetch('/api/v1/admin/team', {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to list team members: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function grantAdminRole(userId: string, role: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/team/${userId}/roles`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ role }),
  });

  if (!response.ok) {
    throw new Error(`Failed to grant role: ${response.statusText}`);
  }
}

export async function revokeAdminRole(userId: string, role: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/team/${userId}/roles/${role}`, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to revoke role: ${response.statusText}`);
  }
}

export type HealthBand = 'Green' | 'Yellow' | 'Red';

export interface TenantHealthScoreResponse {
  id: string;
  tenantId: string;
  tenantName: string;
  overallScore: number;
  billingScore: number;
  caseCompletionScore: number;
  onboardingScore: number;
  healthBand: HealthBand;
  primaryIssue?: string;
  computedAt: string;
}

export async function getHealthScores(): Promise<TenantHealthScoreResponse[]> {
  const response = await fetch('/api/v1/admin/health-scores', {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get health scores: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function triggerHealthScoreCompute(): Promise<void> {
  const response = await fetch('/api/v1/admin/health-scores/compute', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to trigger health score compute: ${response.statusText}`);
  }
}

// ── Dashboard Summary Types ──

export interface TenantCounts {
  total: number;
  active: number;
  trial: number;
  churning: number;
  suspended: number;
}

export interface RevenuePulse {
  mrr: number;
  arr: number;
  churnRate: number;
  growthRate: number;
}

export interface AtRiskTenant {
  tenantId: string;
  name: string;
  score: number;
  primaryIssue: string | null;
}

export interface HealthDistribution {
  green: number;
  yellow: number;
  red: number;
  topAtRisk: AtRiskTenant[];
}

export interface AlertCounts {
  critical: number;
  warning: number;
  info: number;
  unacknowledged: number;
}

export interface OnboardingStatus {
  completionRate: number;
  stalled: number;
}

export interface AdminDashboardSummary {
  computedAt: string;
  tenants: TenantCounts;
  revenue: RevenuePulse;
  health: HealthDistribution;
  alerts: AlertCounts;
  onboarding: OnboardingStatus;
}

export interface DashboardResponse<T> {
  data: T;
  computedAt: string;
  isStale: boolean;
}

export async function getDashboardSummary(): Promise<DashboardResponse<AdminDashboardSummary>> {
  const response = await fetch('/api/v1/admin/dashboard/summary', {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get dashboard summary: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

// ── Tenant Portfolio & 360 Types ──

export interface TenantListItem {
  id: string;
  name: string;
  status: string;
  planTier: string | null;
  healthScore: number | null;
  healthBand: HealthBand | null;
  signupDate: string;
  region: string | null;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TenantListParams {
  name?: string;
  status?: string;
  healthBand?: string;
  planTier?: string;
  signupDateFrom?: string;
  signupDateTo?: string;
  sort?: string;
  order?: string;
  page?: number;
  pageSize?: number;
}

export async function getTenants(params: TenantListParams = {}): Promise<PaginatedResponse<TenantListItem>> {
  const query = new URLSearchParams();
  if (params.name) query.set('name', params.name);
  if (params.status) query.set('status', params.status);
  if (params.healthBand) query.set('healthBand', params.healthBand);
  if (params.planTier) query.set('planTier', params.planTier);
  if (params.signupDateFrom) query.set('signupDateFrom', params.signupDateFrom);
  if (params.signupDateTo) query.set('signupDateTo', params.signupDateTo);
  if (params.sort) query.set('sort', params.sort);
  if (params.order) query.set('order', params.order);
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));

  const qs = query.toString();
  const url = `/api/v1/admin/tenants${qs ? `?${qs}` : ''}`;

  const response = await fetch(url, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get tenants: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

// ── Tenant 360 Types ──

export interface TenantSummary {
  id: string;
  name: string;
  status: string;
  planTier: string | null;
  signupDate: string;
  region: string | null;
  contactEmail: string | null;
  healthScore: number | null;
  healthBand: HealthBand | null;
}

export interface TenantBillingInvoice {
  invoiceNumber: string;
  billingCycleStart: string;
  billingCycleEnd: string;
  totalAmount: number;
  currency: string;
  status: string;
  createdAt: string;
}

export interface TenantBilling {
  subscriptionPlan: string | null;
  billingCycle: string | null;
  subscriptionActivatedAt: string | null;
  billingHealth: string;
  lastPaymentDate: string | null;
  nextRenewalDate: string | null;
  gracePeriodActive: boolean;
  gracePeriodRetryAt: string | null;
  recentInvoices: TenantBillingInvoice[];
}

export interface TenantCaseWorkflowProgress {
  totalSteps: number;
  completedSteps: number;
  inProgressSteps: number;
  blockedSteps: number;
}

export interface TenantCaseBlockedStep {
  stepKey: string;
  title: string;
  blockedReasonCode: string | null;
  blockedReasonDetail: string | null;
}

export interface TenantCaseItem {
  caseId: string;
  caseNumber: string;
  deceasedFullName: string;
  status: string;
  createdAt: string;
  workflowKey: string | null;
  workflowProgress: TenantCaseWorkflowProgress;
  blockedSteps: TenantCaseBlockedStep[];
}

export interface TenantCases {
  cases: TenantCaseItem[];
}

export interface TenantActivityItem {
  eventType: string;
  actorUserId: string;
  timestamp: string;
  caseId: string | null;
  metadata: string;
}

export interface TenantActivity {
  events: TenantActivityItem[];
}

export async function getTenantSummary(tenantId: string): Promise<TenantSummary> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/summary`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get tenant summary: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getTenantBilling(tenantId: string): Promise<TenantBilling> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/billing`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get tenant billing: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getTenantCases(tenantId: string): Promise<TenantCases> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/cases`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get tenant cases: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getTenantActivity(tenantId: string): Promise<TenantActivity> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/activity`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get tenant activity: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}
