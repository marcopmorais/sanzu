import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import AdminQueuesPage from "../../app/app/admin/queues/page";

test("Admin queues page renders queue table", () => {
  const html = renderToStaticMarkup(<AdminQueuesPage />);
  expect(html).toContain("Mission Control Queues");
  expect(html).toContain("Onboarding stuck");
  expect(html).toContain("Failed payment");
  expect(html).toContain("Support escalation");
});

test("Admin queues page renders event stream section", () => {
  const html = renderToStaticMarkup(<AdminQueuesPage />);
  expect(html).toContain("Event Stream Drilldown");
  expect(html).toContain("Privacy-safe summaries");
});
