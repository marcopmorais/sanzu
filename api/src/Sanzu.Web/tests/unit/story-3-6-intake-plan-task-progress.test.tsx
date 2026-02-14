import { describe, it, expect } from "vitest";
import CaseWorkflowPage from "../../app/app/cases/[caseId]/workflow/page";

describe("Story 3.6 intake-plan-task routes", () => {
  it("renders readiness, next best action, and task progression context", () => {
    const tree = CaseWorkflowPage({
      params: { caseId: "00000000-0000-0000-0000-000000000001" }
    });

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("Intake, Plan, and Task Progress");
    expect(JSON.stringify(tree.props)).toContain("readiness");
    expect(JSON.stringify(tree.props)).toContain("Workflow next actions");
  });
});
