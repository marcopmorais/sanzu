import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function ResourcesPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("resources");

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="panel">
        <table className="table" aria-label={t("tableLabel")}>
          <thead>
            <tr>
              <th>{t("columnResource")}</th>
              <th>{t("columnAudience")}</th>
              <th>{t("columnFormat")}</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>{t("onboardingRunbook")}</td>
              <td>{t("agencyAdministrators")}</td>
              <td>{t("formatGuide")}</td>
            </tr>
            <tr>
              <td>{t("complianceChecklist")}</td>
              <td>{t("operationsLeaders")}</td>
              <td>{t("formatChecklist")}</td>
            </tr>
            <tr>
              <td>{t("workflowPrimer")}</td>
              <td>{t("processManagers")}</td>
              <td>{t("formatPlaybook")}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  );
}
