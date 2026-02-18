const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface CaseAuditExportSummary {
  caseNumber: string;
  deceasedFullName: string;
  dateOfDeath: string;
  caseType: string;
  urgency: string;
  status: string;
  playbookId: string | null;
  playbookVersion: number | null;
  createdAt: string;
  closedAt: string | null;
}

export interface CaseAuditExportEvent {
  eventId: string;
  eventType: string;
  actorUserId: string;
  metadata: string;
  createdAt: string;
}

export interface CaseAuditExportEvidenceReference {
  documentId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  classification: string;
  currentVersionNumber: number;
  uploadedAt: string;
}

export interface CaseAuditExportResponse {
  exportId: string;
  caseId: string;
  tenantId: string;
  generatedAt: string;
  generatedByUserId: string;
  caseSummary: CaseAuditExportSummary;
  auditEvents: CaseAuditExportEvent[];
  evidenceReferences: CaseAuditExportEvidenceReference[];
}

export async function exportCaseAudit(
  tenantId: string,
  caseId: string,
): Promise<CaseAuditExportResponse> {
  const res = await fetch(
    `${API_BASE}/api/v1/tenants/${tenantId}/cases/${caseId}/export`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Export fetch failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
