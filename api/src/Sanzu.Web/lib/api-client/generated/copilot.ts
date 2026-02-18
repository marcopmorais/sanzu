const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "";

export interface CopilotExplainabilityBlock {
  basedOn: string;
  reasonCategory: string;
  confidenceBand: string;
  missingOrUnknown: string[];
  safeFallback: string;
}

export interface CopilotDraft {
  id: string;
  draftType: string;
  content: string;
  checklist: string[];
  explainability: CopilotExplainabilityBlock;
  status: string;
  createdAt: string;
}

export interface CopilotDraftAccepted {
  draftId: string;
  status: string;
  acceptedAt: string;
}

export async function requestCopilotDraft(
  tenantId: string,
  body: { draftType: string; caseId: string; workflowStepId?: string; handoffId?: string },
): Promise<CopilotDraft> {
  const res = await fetch(`${API_BASE}/api/v1/tenants/${tenantId}/copilot/draft`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`Draft request failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}

export async function acceptCopilotDraft(
  tenantId: string,
  body: { draftId: string; editedContent?: string },
): Promise<CopilotDraftAccepted> {
  const res = await fetch(`${API_BASE}/api/v1/tenants/${tenantId}/copilot/draft/accept`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`Accept draft failed: ${res.status}`);
  const envelope = await res.json();
  return envelope.data;
}
