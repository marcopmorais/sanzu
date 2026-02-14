export type TenantId = string;
export type CaseId = string;
export type ParticipantId = string;

export async function getCaseDetails(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}`, { method: "GET" });
}

export async function updateCaseLifecycle(tenantId: TenantId, caseId: CaseId, targetState: string) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/lifecycle`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ targetState })
  });
}

export async function inviteCaseParticipant(
  tenantId: TenantId,
  caseId: CaseId,
  email: string,
  role: string
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/participants`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, role })
  });
}

export async function updateCaseParticipantRole(
  tenantId: TenantId,
  caseId: CaseId,
  participantId: ParticipantId,
  role: string
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/participants/${participantId}/role`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ role })
  });
}

export async function submitCaseIntake(tenantId: TenantId, caseId: CaseId, payload: unknown) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/intake`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function generateCasePlan(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/plan/generate`, { method: "POST" });
}

export async function recalculateReadiness(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/plan/readiness/recalculate`, { method: "POST" });
}

export async function getCaseTasks(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/tasks`, { method: "GET" });
}

export async function updateTaskStatus(
  tenantId: TenantId,
  caseId: CaseId,
  stepId: string,
  status: string
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/tasks/${stepId}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status })
  });
}

export async function getCaseTimeline(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/timeline`, { method: "GET" });
}

export async function uploadDocument(tenantId: TenantId, caseId: CaseId, payload: unknown) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getDocument(tenantId: TenantId, caseId: CaseId, documentId: string) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}`, { method: "GET" });
}

export async function uploadDocumentVersion(
  tenantId: TenantId,
  caseId: CaseId,
  documentId: string,
  payload: unknown
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}/versions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getDocumentVersions(tenantId: TenantId, caseId: CaseId, documentId: string) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}/versions`, { method: "GET" });
}

export async function updateDocumentClassification(
  tenantId: TenantId,
  caseId: CaseId,
  documentId: string,
  classification: string
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}/classification`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ classification })
  });
}

export async function extractDocumentCandidates(tenantId: TenantId, caseId: CaseId, documentId: string) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}/extraction/candidates`, {
    method: "POST"
  });
}

export async function applyExtractionReview(tenantId: TenantId, caseId: CaseId, documentId: string, payload: unknown) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/documents/${documentId}/extraction/review`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function generateHandoffPacket(tenantId: TenantId, caseId: CaseId, payload: unknown) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/handoffs/packet`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function getHandoffState(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/handoffs/state`, { method: "GET" });
}

export async function updateHandoffState(
  tenantId: TenantId,
  caseId: CaseId,
  handoffId: string,
  state: string
) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/handoffs/${handoffId}/state`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ state })
  });
}

export async function getProcessAlias(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/process-alias`, { method: "GET" });
}

export async function provisionProcessAlias(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/process-alias/provision`, { method: "POST" });
}

export async function getProcessInbox(tenantId: TenantId, caseId: CaseId) {
  return fetch(`/api/v1/tenants/${tenantId}/cases/${caseId}/process-inbox`, { method: "GET" });
}
