import { describe, it, expect } from "vitest";
import CaseLifecyclePage from "../../app/app/cases/[caseId]/page";

describe("Story 2.5 lifecycle + RBAC routes", () => {
  it("renders lifecycle context, participant table, and blocked-action feedback", () => {
    const tree = CaseLifecyclePage({
      params: { caseId: "00000000-0000-0000-0000-000000000001" }
    });

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("Case Lifecycle and RBAC Collaboration");
    expect(JSON.stringify(tree.props)).toContain("Case participants");
    expect(JSON.stringify(tree.props)).toContain("Reader role cannot archive this case");
  });
});
