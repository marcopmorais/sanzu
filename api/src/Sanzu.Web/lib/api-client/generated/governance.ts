export type TenantId = string;
export type CaseId = string;

export async function getTenantComplianceStatus(tenantId: TenantId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/compliance`, { method: "GET" });
}

export async function getCaseAuditTrail(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/audit`, { method: "GET" });
}

export async function getTenantCaseDefaults(tenantId: TenantId) {
  return fetch(`/api/v1/tenants/${tenantId}/settings/case-defaults`, { method: "GET" });
}

export async function updateTenantCaseDefaults(tenantId: TenantId, payload: unknown) {
  return fetch(`/api/v1/tenants/${tenantId}/settings/case-defaults`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getTenantUsageIndicators(tenantId: TenantId) {
  return fetch(`/api/v1/tenants/${tenantId}/settings/usage-indicators`, { method: "GET" });
}
