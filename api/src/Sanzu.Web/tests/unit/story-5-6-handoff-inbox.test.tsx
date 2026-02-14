import { describe, it, expect } from "vitest";
import HandoffInboxPage from "../../app/app/cases/[caseId]/handoffs/inbox/page";

describe("Story 5.6 handoff and process inbox routes", () => {
  it("renders handoff metadata and role-blocked state controls", () => {
    const tree = HandoffInboxPage({
      params: { caseId: "00000000-0000-0000-0000-000000000001" }
    });

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("External Handoff and Process Inbox");
    expect(JSON.stringify(tree.props)).toContain("Inbox thread metadata");
    expect(JSON.stringify(tree.props)).toContain("Manager action required");
  });
});
