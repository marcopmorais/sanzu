import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import RecoveryPlanPage from "../../app/app/admin/recovery/page";

test("Recovery plan page renders all sections", () => {
  const html = renderToStaticMarkup(<RecoveryPlanPage />);
  expect(html).toContain("Recovery Plans");
  expect(html).toContain("Recovery Plan");
  expect(html).toContain("Explain Why");
  expect(html).toContain("Copilot Boundaries");
});

test("Recovery plan page renders boundary table", () => {
  const html = renderToStaticMarkup(<RecoveryPlanPage />);
  expect(html).toContain("Draft recovery plans");
  expect(html).toContain("Change case lifecycle");
  expect(html).toContain("requires user action");
});
