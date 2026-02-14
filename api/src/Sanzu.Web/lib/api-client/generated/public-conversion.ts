export type DemoRequestPayload = {
  fullName: string;
  email: string;
  organizationName: string;
  teamSize: number;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  referrerPath?: string;
  landingPath?: string;
};

export type StartAccountPayload = {
  agencyName: string;
  adminFullName: string;
  adminEmail: string;
  location: string;
  termsAccepted: boolean;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  referrerPath?: string;
  landingPath?: string;
};

export async function submitDemoRequest(payload: DemoRequestPayload) {
  return fetch("/api/v1/public/demo-request", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}

export async function startAccountCreation(payload: StartAccountPayload) {
  return fetch("/api/v1/public/start-account", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
}
