"use client";

import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { FormEvent, useState } from "react";
import { useTranslations } from "next-intl";
import { startAccountCreation } from "@/lib/api-client/generated/public-conversion";

export default function StartPage() {
  const router = useRouter();
  const t = useTranslations("start");
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
      setError(t("submitError"));
      return;
    }

    router.push("/start/success");
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
              {t("agencyName")}
              <input
                value={agencyName}
                onChange={(event) => setAgencyName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("agencyName")}
              />
            </label>
            <label>
              {t("adminFullName")}
              <input
                value={adminFullName}
                onChange={(event) => setAdminFullName(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("adminFullName")}
              />
            </label>
            <label>
              {t("adminEmail")}
              <input
                value={adminEmail}
                onChange={(event) => setAdminEmail(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("adminEmail")}
              />
            </label>
            <label>
              {t("location")}
              <input
                value={location}
                onChange={(event) => setLocation(event.target.value)}
                style={{ width: "100%", marginTop: 6, padding: 8 }}
                aria-label={t("location")}
              />
            </label>
          </div>
          <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <input type="checkbox" checked={termsAccepted} onChange={(event) => setTermsAccepted(event.target.checked)} />
            {t("acceptTerms")}
          </label>
          <p className="meta">{t("validationNote")}</p>
          {error ? <p className="meta" style={{ color: "var(--warn)" }}>{error}</p> : null}
          <div className="actions">
            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? t("submitting") : t("submitButton")}
            </button>
            <Link href="/start/success">{t("viewSuccess")}</Link>
          </div>
        </form>
        <p className="meta" style={{ marginTop: 8 }}>
          {t.rich("termsConsent", {
            link: (chunks) => <Link href="/legal/terms">{chunks}</Link>,
          })}
        </p>
      </div>
    </main>
  );
}
