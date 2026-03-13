import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function PrivacyPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("legal.privacy");

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="panel">
        <ul className="list-tight">
          <li>{t("dataProcessing")}</li>
          <li>{t("tenantIsolation")}</li>
          <li>{t("auditRetention")}</li>
        </ul>
      </div>
    </main>
  );
}
