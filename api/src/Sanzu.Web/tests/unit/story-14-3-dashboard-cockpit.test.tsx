import { expect, test, describe } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { StalenessIndicator } from "../../components/admin/widgets/StalenessIndicator";
import { TenantSummaryWidget } from "../../components/admin/widgets/TenantSummaryWidget";
import { RevenuePulseWidget } from "../../components/admin/widgets/RevenuePulseWidget";
import { TopAtRiskWidget } from "../../components/admin/widgets/TopAtRiskWidget";
import { AlertInboxWidget } from "../../components/admin/widgets/AlertInboxWidget";
import { OnboardingStatusWidget } from "../../components/admin/widgets/OnboardingStatusWidget";

// ── Staleness Indicator Tests ──

describe("StalenessIndicator", () => {
  test("displays neutral 'Last updated' when not stale", () => {
    const recentTime = new Date(Date.now() - 2 * 60_000).toISOString();
    const html = renderToStaticMarkup(
      <StalenessIndicator computedAt={recentTime} isStale={false} />
    );
    expect(html).toContain("Last updated:");
    expect(html).toContain("min ago");
    expect(html).not.toContain("Data may be stale");
    expect(html).not.toContain("Data is stale");
  });

  test("displays yellow warning when stale but under 5x interval", () => {
    // 15 min ago with default 5 min interval: > 2x (10 min) but < 5x (25 min)
    const staleTime = new Date(Date.now() - 15 * 60_000).toISOString();
    const html = renderToStaticMarkup(
      <StalenessIndicator computedAt={staleTime} isStale={true} />
    );
    expect(html).toContain("Data may be stale");
    expect(html).not.toContain("⚠");
  });

  test("displays red warning when over 5x interval", () => {
    // 30 min ago with default 5 min interval: > 5x (25 min)
    const veryStaleTime = new Date(Date.now() - 30 * 60_000).toISOString();
    const html = renderToStaticMarkup(
      <StalenessIndicator computedAt={veryStaleTime} isStale={true} />
    );
    expect(html).toContain("⚠");
    expect(html).toContain("Data is stale");
  });

  test("includes aria-live polite for accessibility", () => {
    const html = renderToStaticMarkup(
      <StalenessIndicator computedAt={new Date().toISOString()} isStale={false} />
    );
    expect(html).toContain('aria-live="polite"');
  });
});

// ── Widget Render Tests ──

describe("TenantSummaryWidget", () => {
  test("renders tenant counts", () => {
    const html = renderToStaticMarkup(
      <TenantSummaryWidget tenants={{ total: 100, active: 80, trial: 10, churning: 5, suspended: 5 }} />
    );
    expect(html).toContain("Tenant Summary");
    expect(html).toContain("100");
    expect(html).toContain("80");
    expect(html).toContain("Active");
    expect(html).toContain("Churning");
  });
});

describe("RevenuePulseWidget", () => {
  test("renders revenue metrics", () => {
    const html = renderToStaticMarkup(
      <RevenuePulseWidget revenue={{ mrr: 5000, arr: 60000, churnRate: 4.2, growthRate: 8.7 }} />
    );
    expect(html).toContain("Revenue Pulse");
    expect(html).toContain("MRR");
    expect(html).toContain("ARR");
    expect(html).toContain("4.2%");
    expect(html).toContain("8.7%");
  });
});

describe("TopAtRiskWidget", () => {
  test("renders tenant links to Tenant 360 page", () => {
    const topAtRisk = [
      { tenantId: "abc-123", name: "Agency X", score: 22, primaryIssue: "BillingFailed" },
      { tenantId: "def-456", name: "Agency Y", score: 35, primaryIssue: null },
    ];
    const html = renderToStaticMarkup(<TopAtRiskWidget topAtRisk={topAtRisk} />);
    expect(html).toContain("Top At-Risk Tenants");
    expect(html).toContain("Agency X");
    expect(html).toContain("/app/admin/tenants/abc-123");
    expect(html).toContain("/app/admin/tenants/def-456");
    expect(html).toContain("22");
    expect(html).toContain("BillingFailed");
  });

  test("renders empty message when no at-risk tenants", () => {
    const html = renderToStaticMarkup(<TopAtRiskWidget topAtRisk={[]} />);
    expect(html).toContain("No at-risk tenants");
  });
});

describe("AlertInboxWidget", () => {
  test("renders alert counts", () => {
    const html = renderToStaticMarkup(
      <AlertInboxWidget alerts={{ critical: 3, warning: 12, info: 28, unacknowledged: 8 }} />
    );
    expect(html).toContain("Alert Inbox");
    expect(html).toContain("43"); // total
    expect(html).toContain("Critical");
    expect(html).toContain("Warning");
  });
});

describe("OnboardingStatusWidget", () => {
  test("renders onboarding metrics", () => {
    const html = renderToStaticMarkup(
      <OnboardingStatusWidget onboarding={{ completionRate: 87.5, stalled: 4 }} />
    );
    expect(html).toContain("Onboarding Status");
    expect(html).toContain("87.5%");
    expect(html).toContain("4");
  });
});
