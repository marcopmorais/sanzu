import { expect, test, describe } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { HealthGauge } from "../../components/admin/widgets/HealthGauge";

// ── HealthGauge Tests ──

describe("HealthGauge", () => {
  test("renders green color for score >= 70", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={85} band="Green" />
    );
    expect(html).toContain("85");
    expect(html).toContain("Green");
    expect(html).toContain("#1e8f4d");
  });

  test("renders yellow color for score in 40-69 range", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={55} band="Yellow" />
    );
    expect(html).toContain("55");
    expect(html).toContain("Yellow");
    expect(html).toContain("#b85a2a");
  });

  test("renders red color for score < 40", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={20} band="Red" />
    );
    expect(html).toContain("20");
    expect(html).toContain("Red");
    expect(html).toContain("#cc0000");
  });

  test("shows primaryIssue when band is Red", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={15} band="Red" primaryIssue="BillingFailed" />
    );
    expect(html).toContain("BillingFailed");
    expect(html).toContain("health-gauge-issue");
  });

  test("does not show primaryIssue when band is Green", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={90} band="Green" primaryIssue="SomethingMinor" />
    );
    expect(html).not.toContain("health-gauge-issue");
  });

  test("has aria role meter with proper attributes", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={72} band="Green" />
    );
    expect(html).toContain('role="meter"');
    expect(html).toContain('aria-valuenow="72"');
    expect(html).toContain('aria-valuemin="0"');
    expect(html).toContain('aria-valuemax="100"');
  });

  test("renders no-score message when score is null", () => {
    const html = renderToStaticMarkup(
      <HealthGauge score={null} band={null} />
    );
    expect(html).toContain("No health score available");
    expect(html).not.toContain('role="meter"');
  });
});
