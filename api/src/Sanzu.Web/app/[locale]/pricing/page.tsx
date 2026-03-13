import { Link } from "@/i18n/navigation";
import { Button } from "@/components/atoms/Button";
import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function PricingPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("pricing");

  return (
    <main>
      <h1>{t("title")}</h1>
      <p className="meta">{t("subtitle")}</p>
      <div className="grid three">
        <section className="panel">
          <h2>Inicial</h2>
          <p>{t("inicialPrice")}</p>
          <p className="meta">{t("inicialAnnual")}</p>
          <ul className="list-tight">
            <li>{t("inicialCases")}</li>
            <li>{t("inicialOverage")}</li>
            <li>{t("inicialFeatures")}</li>
          </ul>
          <Link href="/start">
            <Button label={t("startFreeTrial")} variant="secondary" />
          </Link>
        </section>
        <section className="panel">
          <h2>Profissional</h2>
          <p>{t("profissionalPrice")}</p>
          <p className="meta">{t("profissionalAnnual")}</p>
          <ul className="list-tight">
            <li>{t("profissionalCases")}</li>
            <li>{t("profissionalOverage")}</li>
            <li>{t("profissionalFeatures")}</li>
          </ul>
          <Link href="/start">
            <Button label={t("startFreeTrial")} />
          </Link>
        </section>
        <section className="panel">
          <h2>Agência</h2>
          <p>{t("agenciaPrice")}</p>
          <p className="meta">{t("agenciaAnnual")}</p>
          <ul className="list-tight">
            <li>{t("agenciaCases")}</li>
            <li>{t("agenciaOverage")}</li>
            <li>{t("agenciaFeatures1")}</li>
            <li>{t("agenciaFeatures2")}</li>
          </ul>
          <Link href="/start">
            <Button label={t("startFreeTrial")} />
          </Link>
        </section>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Enterprise</h2>
        <p className="meta">{t("enterpriseDescription")}</p>
        <div className="actions">
          <Link href="/demo">
            <Button label={t("talkToSales")} />
          </Link>
        </div>
      </div>
      <p className="meta" style={{ marginTop: 14 }}>
        {t("trialNotice")}
      </p>
    </main>
  );
}
