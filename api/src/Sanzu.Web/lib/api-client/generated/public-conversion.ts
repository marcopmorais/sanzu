export async function submitDemoRequest(payload: unknown) {
  return fetch("/api/v1/public/demo-request", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function startAccountCreation(payload: unknown) {
  // Current backend entry point for public conversion into onboarding.
  return fetch("/api/v1/tenants/signup", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}
