import { describe, it, expect } from "vitest";
import GovernanceOversightPage from "../../app/app/governance/oversight/page";

describe("Story 6.5 governance and oversight route", () => {
  it("renders compliance exception context and operational KPI trends", () => {
    const tree = GovernanceOversightPage();

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("Governance and Operational Oversight");
    expect(JSON.stringify(tree.props)).toContain("Retention policy warning");
    expect(JSON.stringify(tree.props)).toContain("Weekly case throughput");
  });
});
