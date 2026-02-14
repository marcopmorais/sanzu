"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { submitDemoRequest } from "@/lib/api-client/generated/public-conversion";

export default function DemoPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState("Marina Rivera");
  const [email, setEmail] = useState("ops@agencyexample.com");
  const [organizationName, setOrganizationName] = useState("Agency Example");
  const [teamSize, setTeamSize] = useState(25);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    const search = new URLSearchParams(window.location.search);

    const response = await submitDemoRequest({
      fullName,
      email,
      organizationName,
      teamSize,
      utmSource: search.get("utm_source") ?? undefined,
      utmMedium: search.get("utm_medium") ?? undefined,
      utmCampaign: search.get("utm_campaign") ?? undefined,
      referrerPath: document.referrer || undefined,
      landingPath: window.location.pathname
    });

    setIsSubmitting(false);

    if (!response.ok) {
      setError("Unable to submit demo request. Please verify required fields and try again.");
      return;
    }

    router.push("/demo/success");
  };

  return (
    <main>
      <h1>Request a Demo</h1>
      <p className="meta">Public conversion route for qualified demo intent.</p>
      <div className="panel">
        <p>Tell us your team context so we can tailor the walkthrough.</p>
        <form onSubmit={handleSubmit} className="grid" aria-label="Demo request form">
          <div className="grid two">
            <label>
              Full name
              <input
                value={fullName}
                onChange={(event) => setFullName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Full name"
              />
            </label>
            <label>
              Work email
              <input
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Work email"
              />
            </label>
            <label>
              Organization
              <input
                value={organizationName}
                onChange={(event) => setOrganizationName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Organization"
              />
            </label>
            <label>
              Team size
              <input
                type="number"
                min={1}
                value={teamSize}
                onChange={(event) => setTeamSize(Number(event.target.value))}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label="Team size"
              />
            </label>
          </div>
          <p className="meta">Validation: business email and consent acknowledgment are required before submit.</p>
          {error ? <p className="meta" style={{ color: "var(--warn)" }}>{error}</p> : null}
          <div className="actions">
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Submitting..." : "Submit Demo Request"}
            </button>
            <Link href="/demo/success">View Success State</Link>
          </div>
        </form>
        <p className="meta" style={{ marginTop: 8 }}>
          By submitting you agree to Sanzu contact processing under{" "}
          <Link href="/legal/privacy">Privacy Policy</Link>.
        </p>
      </div>
    </main>
  );
}
