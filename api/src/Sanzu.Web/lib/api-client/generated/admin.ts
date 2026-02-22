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
