const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface RemediationImpactPreview {
  actionType: string;
  impactSummary: string;
  isReversible: boolean;
  affectedEntities: string[];
}

export interface RemediationAction {
  id: string;
  queueId: string;
  queueItemId: string;
  tenantId: string;
  actionType: string;
  auditNote: string;
  impactSummary: string | null;
  status: string;
  verificationType: string | null;
  verificationResult: string | null;
  createdAt: string;
  committedAt: string | null;
  verifiedAt: string | null;
  resolvedAt: string | null;
}

export async function previewRemediation(
  actionType: string,
  tenantId: string,
): Promise<RemediationImpactPreview> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/remediation/preview?actionType=${actionType}&tenantId=${tenantId}`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Preview failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function commitRemediation(body: {
  queueId: string;
  queueItemId: string;
  tenantId: string;
  actionType: string;
  auditNote: string;
}): Promise<RemediationAction> {
  const res = await fetch(`${API_BASE}/api/v1/admin/remediation/commit`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`Commit failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function verifyRemediation(
  remediationId: string,
  body: { verificationType: string; verificationResult?: string; passed: boolean },
): Promise<RemediationAction> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/remediation/${remediationId}/verify`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    },
  );
  if (!res.ok) throw new Error(`Verify failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function resolveRemediation(
  remediationId: string,
  body: { overrideNote?: string },
): Promise<RemediationAction> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/remediation/${remediationId}/resolve`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    },
  );
  if (!res.ok) throw new Error(`Resolve failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
