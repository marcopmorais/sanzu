"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { startAccountCreation } from "@/lib/api-client/generated/public-conversion";

export default function StartPage() {
  const router = useRouter();
  const [agencyName, setAgencyName] = useState("Horizon Family Partners");
  const [adminFullName, setAdminFullName] = useState("Agency Owner");
  const [adminEmail, setAdminEmail] = useState("admin@horizonfp.com");
  const [location, setLocation] = useState("Lisbon");
  const [termsAccepted, setTermsAccepted] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    const search = new URLSearchParams(window.location.search);

    const response = await startAccountCreation({
      agencyName,
      adminFullName,
      adminEmail,
      location,
      termsAccepted,
      utmSource: search.get("utm_source") ?? undefined,
      utmMedium: search.get("utm_medium") ?? undefined,
      utmCampaign: search.get("utm_campaign") ?? undefined,
      referrerPath: document.referrer || undefined,
      landingPath: window.location.pathname
    });

    setIsSubmitting(false);

    if (!response.ok) {
      setError("Unable to start account creation. Check required fields and terms acceptance.");
      return;
    }

    router.push("/start/success");
  };

  return (
    <main>
      <h1>Start with Sanzu</h1>
      <p className="meta">Public conversion route that starts account creation and onboarding.</p>
      <div className="panel">
        <p>Create your tenant workspace and primary admin profile.</p>
        <form onSubmit={handleSubmit} className="grid" aria-label="Start account form">
          <div className="grid two">
            <label>
              Agency name
              <input
                value={agencyName}
                onChange={(event) => setAgencyName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Agency name"
              />
            </label>
            <label>
              Administrator full name
              <input
                value={adminFullName}
                onChange={(event) => setAdminFullName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Administrator full name"
              />
            </label>
            <label>
              Administrator email
              <input
                value={adminEmail}
                onChange={(event) => setAdminEmail(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Administrator email"
              />
            </label>
            <label>
              Location
              <input
                value={location}
                onChange={(event) => setLocation(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Location"
              />
            </label>
          </div>
          <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <input type="checkbox" checked={termsAccepted} onChange={(event) => setTermsAccepted(event.target.checked)} />
            I accept the Terms of Service
          </label>
          <p className="meta">Validation: agency name, business email, and terms consent are required.</p>
          {error ? <p className="meta" style={{ color: "var(--warn)" }}>{error}</p> : null}
          <div className="actions">
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Submitting..." : "Create Account"}
            </button>
            <Link href="/start/success">View Success State</Link>
          </div>
        </form>
        <p className="meta" style={{ marginTop: 8 }}>
          Continuing means you accept the <Link href="/legal/terms">Terms of Service</Link>.
        </p>
      </div>
    </main>
  );
}
