export type TenantId = string;

export async function updateTenantLifecycleState(tenantId: TenantId, payload: unknown) {
  return fetch(`/api/v1/admin/tenants/${tenantId}/lifecycle`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function startSupportDiagnosticSession(tenantId: TenantId, payload: unknown) {
  return fetch(`/api/v1/admin/tenants/${tenantId}/diagnostics/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getSupportDiagnosticSummary(tenantId: TenantId, sessionId: string) {
  return fetch(`/api/v1/admin/tenants/${tenantId}/diagnostics/sessions/${sessionId}/summary`, { method: "GET" });
}

export async function applyTenantPolicyControl(tenantId: TenantId, payload: unknown) {
  return fetch(`/api/v1/admin/tenants/${tenantId}/policy-controls`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getKpiDashboard(tenantLimit = 10) {
  return fetch(`/api/v1/admin/kpi/dashboard?tenantLimit=${tenantLimit}`, { method: "GET" });
}

export async function upsertKpiThreshold(payload: unknown) {
  return fetch("/api/v1/admin/kpi/thresholds", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function evaluateKpiAlerts(payload: unknown) {
  return fetch("/api/v1/admin/kpi/alerts/evaluate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}
