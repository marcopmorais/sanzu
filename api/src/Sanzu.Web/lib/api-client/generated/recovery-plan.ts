const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface RecoveryStep {
  order: number;
  description: string;
  owner: string;
}

export interface RecoveryEscalation {
  targetRole: string;
  instruction: string;
}

export interface RecoveryPlan {
  id: string;
  caseId: string;
  reasonCategory: string;
  reasonLabel: string;
  explanation: string;
  steps: RecoveryStep[];
  evidenceChecklist: string[];
  escalation: RecoveryEscalation;
  explainability: {
    basedOn: string;
    reasonCategory: string;
    confidenceBand: string;
    missingOrUnknown: string[];
    safeFallback: string;
  };
  boundaryMessage: string;
  createdAt: string;
}

export async function requestRecoveryPlan(
  tenantId: string,
  body: { caseId: string; workflowStepId?: string },
): Promise<RecoveryPlan> {
  const res = await fetch(
    `${API_BASE}/api/v1/tenants/${tenantId}/copilot/recovery-plan`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    },
  );
  if (!res.ok) throw new Error(`Recovery plan failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function requestAdminRecoveryPlan(
  tenantId: string,
  caseId: string,
): Promise<RecoveryPlan> {
  const res = await fetch(
    `${API_BASE}/api/v1/admin/recovery/${tenantId}/cases/${caseId}/plan`,
    { cache: "no-store" },
  );
  if (!res.ok) throw new Error(`Admin recovery plan failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
