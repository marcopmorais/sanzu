"use client";

import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { FormEvent, useState } from "react";
import { useTranslations } from "next-intl";
import { submitDemoRequest } from "@/lib/api-client/generated/public-conversion";

export default function DemoPage() {
  const router = useRouter();
  const t = useTranslations("demo");
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
      setError(t("submitError"));
      return;
    }

    router.push("/demo/success");
  };

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="panel">
        <p>{t("description")}</p>
        <form onSubmit={handleSubmit} className="grid" aria-label={t("formLabel")}>
          <div className="grid two">
            <label>
              {t("fullName")}
              <input
                value={fullName}
                onChange={(event) => setFullName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("fullName")}
              />
            </label>
            <label>
              {t("workEmail")}
              <input
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("workEmail")}
              />
            </label>
            <label>
              {t("organization")}
              <input
                value={organizationName}
                onChange={(event) => setOrganizationName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("organization")}
              />
            </label>
            <label>
              {t("teamSize")}
              <input
                type="number"
                min={1}
                value={teamSize}
                onChange={(event) => setTeamSize(Number(event.target.value))}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("teamSize")}
              />
            </label>
          </div>
          <p className="meta">{t("validationNote")}</p>
          {error ? <p className="meta" style={{ color: "var(--warn)" }}>{error}</p> : null}
          <div className="actions">
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? t("submitting") : t("submitButton")}
            </button>
            <Link href="/demo/success">{t("viewSuccess")}</Link>
          </div>
        </form>
        <p className="meta" style={{ marginTop: 8 }}>
          {t.rich("privacyConsent", {
            link: (chunks) => <Link href="/legal/privacy">{chunks}</Link>,
          })}
        </p>
      </div>
    </main>
  );
}
