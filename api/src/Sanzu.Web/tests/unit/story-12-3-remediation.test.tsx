import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import RemediationPage from "../../app/app/admin/remediation/page";

test("Remediation page renders workflow sections", () => {
  const html = renderToStaticMarkup(<RemediationPage />);
  expect(html).toContain("Remediation Actions");
  expect(html).toContain("Impact Preview");
  expect(html).toContain("Verification");
  expect(html).toContain("Action Catalog");
});

test("Remediation page renders action catalog", () => {
  const html = renderToStaticMarkup(<RemediationPage />);
  expect(html).toContain("Contact tenant");
  expect(html).toContain("Suspend tenant");
  expect(html).toContain("Run diagnostics");
});
