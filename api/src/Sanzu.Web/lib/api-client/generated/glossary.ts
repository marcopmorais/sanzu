/**
 * Glossary API Client
 * Auto-generated - do not manually edit
 */

export interface GlossaryTermResponse {
  key: string;
  term: string;
  definition: string;
  whyThisMatters?: string;
  locale: string;
  visibility: 'Public' | 'AgencyOnly' | 'AdminOnly';
  updatedAt: string;
}

export interface GlossaryLookupResponse {
  terms: GlossaryTermResponse[];
}

export interface UpsertGlossaryTermRequest {
  term: string;
  definition: string;
  whyThisMatters?: string;
  locale?: string;
  visibility: 'Public' | 'AgencyOnly' | 'AdminOnly';
}

export async function searchGlossaryTerms(
  tenantId: string,
  query: string,
  locale?: string
): Promise<GlossaryLookupResponse> {
  const params = new URLSearchParams();
  if (query) params.append('q', query);
  if (locale) params.append('locale', locale);

  const response = await fetch(
    `/api/v1/tenants/${tenantId}/glossary/search?${params}`,
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to search glossary: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function getGlossaryTerm(
  tenantId: string,
  key: string,
  locale?: string
): Promise<GlossaryTermResponse> {
  const params = new URLSearchParams();
  if (locale) params.append('locale', locale);

  const response = await fetch(
    `/api/v1/tenants/${tenantId}/glossary/${encodeURIComponent(key)}?${params}`,
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to get glossary term: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}

export async function upsertGlossaryTerm(
  tenantId: string,
  key: string,
  request: UpsertGlossaryTermRequest
): Promise<GlossaryTermResponse> {
  const response = await fetch(
    `/api/v1/tenants/${tenantId}/glossary/${encodeURIComponent(key)}`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  if (!response.ok) {
    throw new Error(`Failed to upsert glossary term: ${response.statusText}`);
  }

  const envelope = await response.json();
  return envelope.data;
}
