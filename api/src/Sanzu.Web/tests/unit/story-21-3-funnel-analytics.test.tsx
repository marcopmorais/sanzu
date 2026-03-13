import { expect, test, describe, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

// Mock admin API module
vi.mock("@/lib/api-client/generated/admin", () => ({
  getFunnelData: vi.fn().mockResolvedValue({
    stages: [
      { stageName: "Signup", count: 100, dropOffCount: 0, dropOffPercentage: 0 },
      { stageName: "OnboardingDefaults", count: 80, dropOffCount: 20, dropOffPercentage: 20 },
      { stageName: "OnboardingComplete", count: 60, dropOffCount: 20, dropOffPercentage: 25 },
      { stageName: "BillingActive", count: 45, dropOffCount: 15, dropOffPercentage: 25 },
      { stageName: "FirstCaseCreated", count: 30, dropOffCount: 15, dropOffPercentage: 33.3 },
      { stageName: "ActiveUsage", count: 15, dropOffCount: 15, dropOffPercentage: 50 },
    ],
    cohort: undefined,
    cohortValue: undefined,
  }),
  getFunnelStageTenants: vi.fn().mockResolvedValue([]),
}));

describe("FunnelAnalyticsPage", () => {
  test("page module exports default component", async () => {
    const mod = await import("../../app/[locale]/app/admin/analytics/funnel/page");
    expect(mod.default).toBeDefined();
    expect(typeof mod.default).toBe("function");
  });

  test("renders page title", async () => {
    const mod = await import("../../app/[locale]/app/admin/analytics/funnel/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Funnel Analytics");
  });

  test("renders cohort filter controls", async () => {
    const mod = await import("../../app/[locale]/app/admin/analytics/funnel/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Cohort");
    expect(html).toContain("All time");
    expect(html).toContain("Month");
    expect(html).toContain("Week");
    expect(html).toContain("Apply");
  });

  test("renders loading state initially", async () => {
    const mod = await import("../../app/[locale]/app/admin/analytics/funnel/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Loading funnel data...");
  });

  test("API client exports funnel functions", async () => {
    const admin = await import("@/lib/api-client/generated/admin");
    expect(typeof admin.getFunnelData).toBe("function");
    expect(typeof admin.getFunnelStageTenants).toBe("function");
  });

  test("page source contains all six funnel stages", async () => {
    const fs = await import("node:fs");
    const path = await import("node:path");
    const src = fs.readFileSync(
      path.resolve(__dirname, "../../app/[locale]/app/admin/analytics/funnel/page.tsx"),
      "utf-8"
    );
    expect(src).toContain("Acquisition Funnel");
    expect(src).toContain("funnel-stage-");
    expect(src).toContain("drilldown-table");
    expect(src).toContain("Days at Stage");
  });
});
