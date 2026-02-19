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
