/**
 * Playbooks API Client
 * Auto-generated - do not manually edit
 */

export type PlaybookStatus = 'Draft' | 'InReview' | 'Active' | 'Archived';

export interface PlaybookResponse {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  version: number;
  status: PlaybookStatus;
  changeNotes?: string;
  createdByUserId: string;
  activatedByUserId?: string;
  createdAt: string;
  updatedAt: string;
  activatedAt?: string;
}

export interface CreatePlaybookRequest {
  name: string;
  description?: string;
  changeNotes?: string;
}

export interface UpdatePlaybookRequest {
  name?: string;
  description?: string;
  changeNotes?: string;
  status?: PlaybookStatus;
}

export async function listPlaybooks(
  tenantId: string
): Promise<PlaybookResponse[]> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/settings/playbooks`,
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to list playbooks: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getPlaybook(
  tenantId: string,
  playbookId: string
): Promise<PlaybookResponse> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/settings/playbooks/${playbookId}`,
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to get playbook: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function createPlaybook(
  tenantId: string,
  request: CreatePlaybookRequest
): Promise<PlaybookResponse> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/settings/playbooks`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to create playbook: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function updatePlaybook(
  tenantId: string,
  playbookId: string,
  request: UpdatePlaybookRequest
): Promise<PlaybookResponse> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/settings/playbooks/${playbookId}`,
    {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to update playbook: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function activatePlaybook(
  tenantId: string,
  playbookId: string
): Promise<PlaybookResponse> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/settings/playbooks/${playbookId}/activate`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to activate playbook: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}
