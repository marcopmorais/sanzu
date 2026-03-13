import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function TermsPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("legal.terms");

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="panel">
        <ul className="list-tight">
          <li>{t("authorizedUse")}</li>
          <li>{t("accessControls")}</li>
          <li>{t("billingTerms")}</li>
        </ul>
      </div>
    </main>
  );
}
