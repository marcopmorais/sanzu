import { expect, test, describe } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { TrendLineChart } from "../../components/admin/charts/TrendLineChart";
import { PlanDistributionChart } from "../../components/admin/charts/PlanDistributionChart";

describe("TrendLineChart", () => {
  test("renders empty state when no data", () => {
    const html = renderToStaticMarkup(<TrendLineChart data={[]} />);
    expect(html).toContain("No trend data available");
  });

  test("renders with data without throwing", () => {
    const data = [
      { periodLabel: "2026-01", mrr: 500, tenantCount: 3 },
      { periodLabel: "2026-02", mrr: 750, tenantCount: 5 },
    ];
    // Recharts uses browser APIs; renderToStaticMarkup may produce minimal output
    // but should not throw
    expect(() => renderToStaticMarkup(<TrendLineChart data={data} />)).not.toThrow();
  });

  test("includes aria-label for accessibility", () => {
    const data = [
      { periodLabel: "2026-01", mrr: 500, tenantCount: 3 },
    ];
    const html = renderToStaticMarkup(<TrendLineChart data={data} />);
    expect(html).toContain("aria-label");
    expect(html).toContain("MRR trend chart");
  });
});

describe("PlanDistributionChart", () => {
  test("renders empty state when no data", () => {
    const html = renderToStaticMarkup(<PlanDistributionChart data={[]} />);
    expect(html).toContain("No plan distribution data available");
  });

  test("renders with data without throwing", () => {
    const data = [
      { planName: "Inicial", tenantCount: 5, mrr: 745, percentage: 65.2 },
      { planName: "Profissional", tenantCount: 2, mrr: 798, percentage: 34.8 },
    ];
    expect(() => renderToStaticMarkup(<PlanDistributionChart data={data} />)).not.toThrow();
  });

  test("includes sr-only text for accessibility", () => {
    const data = [
      { planName: "Inicial", tenantCount: 5, mrr: 745, percentage: 65.2 },
    ];
    const html = renderToStaticMarkup(<PlanDistributionChart data={data} />);
    expect(html).toContain("sr-only");
  });

  test("includes data-testid attribute", () => {
    const data = [
      { planName: "Inicial", tenantCount: 5, mrr: 745, percentage: 65.2 },
    ];
    const html = renderToStaticMarkup(<PlanDistributionChart data={data} />);
    expect(html).toContain("plan-distribution-chart");
  });
});
