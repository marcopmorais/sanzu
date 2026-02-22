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

// ── Revenue & Billing ──

export interface RevenueOverview {
  mrr: number;
  arr: number;
  churnRate: number;
  growthRate: number;
  planBreakdown: PlanRevenueItem[];
}

export interface PlanRevenueItem {
  planName: string;
  tenantCount: number;
  mrr: number;
  percentage: number;
}

export interface RevenueTrends {
  dataPoints: RevenueTrendPoint[];
}

export interface RevenueTrendPoint {
  periodLabel: string;
  mrr: number;
  tenantCount: number;
}

export interface BillingHealth {
  failedPaymentCount: number;
  overdueInvoiceCount: number;
  gracePeriodCount: number;
  failedPayments: BillingHealthTenantItem[];
  gracePeriodTenants: BillingHealthTenantItem[];
  upcomingRenewals: BillingHealthTenantItem[];
}

export interface BillingHealthTenantItem {
  tenantId: string;
  tenantName: string;
  failedAmount?: number;
  lastFailedAt?: string;
  gracePeriodRetryAt?: string;
  nextRenewalDate?: string;
}

export async function getRevenueOverview(): Promise<RevenueOverview> {
  const response = await fetch('/api/v1/admin/revenue', {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get revenue overview: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getRevenueTrends(period: string = 'monthly'): Promise<RevenueTrends> {
  const response = await fetch(`/api/v1/admin/revenue/trends?period=${period}`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get revenue trends: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getBillingHealth(): Promise<BillingHealth> {
  const response = await fetch('/api/v1/admin/revenue/billing-health', {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get billing health: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function exportRevenueCsv(): Promise<void> {
  const response = await fetch('/api/v1/admin/revenue/export', {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to export revenue CSV: ${response.statusText}`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'revenue-export.csv';
  a.click();
  URL.revokeObjectURL(url);
}

// ── Alerts ──

export interface AdminAlertItem {
  id: string;
  tenantId?: string;
  alertType: string;
  severity: string;
  title: string;
  detail: string;
  status: string;
  routedToRole: string;
  ownedByUserId?: string;
  firedAt: string;
  acknowledgedAt?: string;
  resolvedAt?: string;
  tenantName?: string;
}

export interface AlertFilters {
  status?: string;
  severity?: string;
  alertType?: string;
}

export async function getAlerts(filters?: AlertFilters): Promise<AdminAlertItem[]> {
  const params = new URLSearchParams();
  if (filters?.status) params.set('status', filters.status);
  if (filters?.severity) params.set('severity', filters.severity);
  if (filters?.alertType) params.set('alertType', filters.alertType);

  const url = `/api/v1/admin/alerts${params.toString() ? '?' + params.toString() : ''}`;
  const response = await fetch(url, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to get alerts: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function updateAlertStatus(alertId: string, status: 'Acknowledged' | 'Resolved'): Promise<void> {
  const response = await fetch(`/api/v1/admin/alerts/${alertId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status }),
  });

  if (!response.ok) {
    throw new Error(`Failed to update alert: ${response.statusText}`);
  }
}

// ── Audit ──

export interface AuditEventItem {
  id: string;
  actorUserId: string;
  actorName: string;
  eventType: string;
  caseId?: string;
  metadata: string;
  timestamp: string;
}

export interface AuditSearchResult {
  items: AuditEventItem[];
  nextCursor?: string;
  totalCount: number;
}

export interface AuditFilters {
  actorUserId?: string;
  eventType?: string;
  caseId?: string;
  dateFrom?: string;
  dateTo?: string;
  cursor?: string;
  pageSize?: number;
}

export async function searchAuditEvents(filters?: AuditFilters): Promise<AuditSearchResult> {
  const params = new URLSearchParams();
  if (filters?.actorUserId) params.set('actorUserId', filters.actorUserId);
  if (filters?.eventType) params.set('eventType', filters.eventType);
  if (filters?.caseId) params.set('caseId', filters.caseId);
  if (filters?.dateFrom) params.set('dateFrom', filters.dateFrom);
  if (filters?.dateTo) params.set('dateTo', filters.dateTo);
  if (filters?.cursor) params.set('cursor', filters.cursor);
  if (filters?.pageSize) params.set('pageSize', String(filters.pageSize));

  const url = `/api/v1/admin/audit${params.toString() ? '?' + params.toString() : ''}`;
  const response = await fetch(url, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Failed to search audit events: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function exportAuditEvents(format: 'csv' | 'json', filters?: AuditFilters): Promise<void> {
  const params = new URLSearchParams();
  params.set('format', format);
  if (filters?.actorUserId) params.set('actorUserId', filters.actorUserId);
  if (filters?.eventType) params.set('eventType', filters.eventType);
  if (filters?.caseId) params.set('caseId', filters.caseId);
  if (filters?.dateFrom) params.set('dateFrom', filters.dateFrom);
  if (filters?.dateTo) params.set('dateTo', filters.dateTo);

  const response = await fetch(`/api/v1/admin/audit/export?${params.toString()}`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to export audit events: ${response.statusText}`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `audit-export.${format}`;
  a.click();
  URL.revokeObjectURL(url);
}

// ── Support Actions ──

export async function overrideBlockedStep(tenantId: string, caseId: string, stepId: string, rationale: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/actions/override-blocked-step`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ caseId, stepId, rationale }),
  });
  if (!response.ok) throw new Error(`Failed to override blocked step: ${response.statusText}`);
}

export async function extendGracePeriod(tenantId: string, days: number, rationale: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/actions/extend-grace-period`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ days, rationale }),
  });
  if (!response.ok) throw new Error(`Failed to extend grace period: ${response.statusText}`);
}

export async function triggerReOnboarding(tenantId: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/actions/re-onboard`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  });
  if (!response.ok) throw new Error(`Failed to trigger re-onboarding: ${response.statusText}`);
}

export interface ImpersonationResult {
  token: string;
  expiresAt: string;
  tenantId: string;
  tenantName: string;
}

export async function startImpersonation(tenantId: string): Promise<ImpersonationResult> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/actions/impersonate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  });
  if (!response.ok) throw new Error(`Failed to start impersonation: ${response.statusText}`);
  const envelope = await response.json();
  return envelope.data;
}

export interface CommItem {
  id: string;
  tenantId: string;
  senderUserId: string;
  senderName: string;
  messageType: string;
  subject: string;
  body: string;
  createdAt: string;
}

export async function sendCommunication(tenantId: string, subject: string, body: string, messageType: string, templateId?: string): Promise<void> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/actions/send-communication`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ subject, body, messageType, templateId }),
  });
  if (!response.ok) throw new Error(`Failed to send communication: ${response.statusText}`);
}

export async function getTenantComms(tenantId: string): Promise<CommItem[]> {
  const response = await fetch(`/api/v1/admin/tenants/${tenantId}/comms`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' },
  });
  if (!response.ok) throw new Error(`Failed to get tenant comms: ${response.statusText}`);
  const envelope = await response.json();
  return envelope.data;
}

export async function exportBillingHealthCsv(): Promise<void> {
  const response = await fetch('/api/v1/admin/revenue/billing-health/export', {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to export billing health CSV: ${response.statusText}`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'billing-health-export.csv';
  a.click();
  URL.revokeObjectURL(url);
}
