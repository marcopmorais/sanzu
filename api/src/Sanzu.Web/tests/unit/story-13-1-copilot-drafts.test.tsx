import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import CopilotDraftPage from "../../app/app/cases/[caseId]/copilot/page";

test("Copilot draft page renders all sections", () => {
  const html = renderToStaticMarkup(<CopilotDraftPage />);
  expect(html).toContain("Copilot Drafts");
  expect(html).toContain("Evidence Request");
  expect(html).toContain("Handoff Checklist");
  expect(html).toContain("Explainability");
});

test("Copilot draft page renders explainability fields", () => {
  const html = renderToStaticMarkup(<CopilotDraftPage />);
  expect(html).toContain("Based on");
  expect(html).toContain("Reason category");
  expect(html).toContain("Confidence band");
  expect(html).toContain("Safe fallback");
});
