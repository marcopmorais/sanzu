import { Link } from "@/i18n/navigation";
import { Button } from "@/components/atoms/Button";
import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("home");

  return (
    <main>
      <div className="hero">
        <h1>Sanzu</h1>
        <p className="meta">{t("tagline")}</p>
        <div className="actions">
          <Link href="/demo">
            <Button label={t("bookDemo")} />
          </Link>
          <Link href="/start">
            <Button label={t("startOnboarding")} variant="secondary" />
          </Link>
        </div>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>{t("explore")}</h2>
        <div className="actions">
          <Link href="/trust">{t("trustLink")}</Link>
          <Link href="/pricing">{t("pricingLink")}</Link>
          <Link href="/resources">{t("resourcesLink")}</Link>
          <Link href="/legal/privacy">{t("privacyLink")}</Link>
          <Link href="/legal/terms">{t("termsLink")}</Link>
        </div>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>{t("storyRoutes")}</h2>
        <ul>
          <li>
            <Link href="/app/onboarding">{t("onboardingLink")}</Link>
          </li>
          <li>
            <Link href="/app/onboarding/billing">{t("billingActivationLink")}</Link>
          </li>
          <li>
            <Link href="/app/settings/billing/history">{t("billingHistoryLink")}</Link>
          </li>
          <li>
            <Link href="/app/settings/billing/recovery">{t("paymentRecoveryLink")}</Link>
          </li>
        </ul>
      </div>
    </main>
  );
}
