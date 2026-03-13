import { describe, it, expect, vi } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import enMessages from "../../messages/en-GB.json";

function createTranslationFn(namespace: string) {
  const parts = namespace.split(".");
  let messages: Record<string, unknown> = enMessages;
  for (const part of parts) {
    messages = messages[part] as Record<string, unknown>;
  }
  const t = (key: string) => (messages[key] as string) ?? key;
  t.rich = (key: string, _opts?: Record<string, unknown>) => (messages[key] as string) ?? key;
  return t;
}

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
  usePathname: () => "/",
  notFound: vi.fn()
}));

vi.mock("next-intl", () => ({
  useTranslations: (ns: string) => createTranslationFn(ns),
  useLocale: () => "en-GB",
  hasLocale: () => true,
  NextIntlClientProvider: ({ children }: { children: React.ReactNode }) => children
}));

vi.mock("next-intl/server", () => ({
  getTranslations: async (nsOrOpts: string | { locale: string; namespace: string }) => {
    const ns = typeof nsOrOpts === "string" ? nsOrOpts : nsOrOpts.namespace;
    return createTranslationFn(ns);
  },
  setRequestLocale: () => {}
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) =>
    createElement("a", { href }, children),
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
  usePathname: () => "/",
  redirect: vi.fn(),
  getPathname: vi.fn()
}));

vi.mock("@/i18n/routing", () => ({
  routing: { locales: ["pt-PT", "en-GB", "fr-FR"], defaultLocale: "pt-PT" }
}));

import DemoPage from "../../app/[locale]/demo/page";
import StartPage from "../../app/[locale]/start/page";

describe("Story 8.5 public web conversion routes", () => {
  it("renders public trust-to-conversion journey copy and legal cues", async () => {
    const { default: HomePage } = await import("../../app/[locale]/page");
    const homeJsx = await HomePage({ params: Promise.resolve({ locale: "en-GB" }) });
    const homeHtml = renderToStaticMarkup(homeJsx);

    const demoHtml = renderToStaticMarkup(createElement(DemoPage));
    const startHtml = renderToStaticMarkup(createElement(StartPage));

    // Verify actual translated content renders (not just key names)
    expect(homeHtml).toContain("Sanzu");
    expect(homeHtml).toContain("Privacy");
    expect(homeHtml).toContain("Trusted case workflow platform");

    expect(demoHtml).toContain("Request a Demo");
    expect(demoHtml).toContain("Privacy Policy");

    expect(startHtml).toContain("Start with Sanzu");
    expect(startHtml).toContain("Terms of Service");
  });
});
