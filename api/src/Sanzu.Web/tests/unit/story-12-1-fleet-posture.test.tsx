import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import FleetPosturePage from "../../app/app/admin/fleet/page";
import TenantDrilldownPage from "../../app/app/admin/fleet/[tenantId]/page";

test("Fleet posture page renders key sections", () => {
  const html = renderToStaticMarkup(<FleetPosturePage />);
  expect(html).toContain("Tenant Fleet Posture");
  expect(html).toContain("Fleet overview");
  expect(html).toContain("Total tenants");
  expect(html).toContain("Blocked Tasks");
});

test("Tenant drilldown page renders metrics and blocked-by-reason", () => {
  const html = renderToStaticMarkup(<TenantDrilldownPage />);
  expect(html).toContain("Tenant Drilldown");
  expect(html).toContain("Metrics");
  expect(html).toContain("Blocked by Reason");
  expect(html).toContain("Back to Fleet");
});
