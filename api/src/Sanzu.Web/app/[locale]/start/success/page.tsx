import { Link } from "@/i18n/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";

export default async function StartSuccessPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("start");

  return (
    <main>
      <h1>{t("successTitle")}</h1>
      <p className="meta">{t("successSubtitle")}</p>
      <div className="panel">
        <p>{t("successMessage")}</p>
        <p className="meta">
          {t.rich("successNext", {
            link: (chunks) => <Link href="/resources">{chunks}</Link>,
          })}
        </p>
      </div>
    </main>
  );
}
