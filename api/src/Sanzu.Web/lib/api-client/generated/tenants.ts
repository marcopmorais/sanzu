export type TenantId = string;

export interface BillingActivationRequest {
  planTier: string;
  paymentMethodToken: string;
}

export async function activateBilling(tenantId: TenantId, request: BillingActivationRequest) {
  return fetch(`/api/v1/tenants/${tenantId}/onboarding/billing/activate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });
}

export async function getBillingHistory(tenantId: TenantId) {
  return fetch(`/api/v1/tenants/${tenantId}/billing/history`, { method: "GET" });
}

export async function executePaymentRecovery(tenantId: TenantId, paymentIntentId: string) {
  return fetch(`/api/v1/tenants/${tenantId}/billing/recovery/execute`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paymentIntentId })
  });
}
