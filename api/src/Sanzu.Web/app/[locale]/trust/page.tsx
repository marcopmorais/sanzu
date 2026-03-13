import { Link } from "@/i18n/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function TrustPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("trust");

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="panel">
        <ul className="list-tight">
          <li>{t("tenantIsolation")}</li>
          <li>{t("auditability")}</li>
          <li>{t("policyControls")}</li>
        </ul>
        <p className="meta" style={{ marginTop: 8 }}>
          {t.rich("legalLinks", {
            privacy: (chunks) => <Link href="/legal/privacy">{chunks}</Link>,
            terms: (chunks) => <Link href="/legal/terms">{chunks}</Link>,
          })}
        </p>
      </div>
    </main>
  );
}
