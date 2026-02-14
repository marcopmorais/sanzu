import { describe, it, expect } from "vitest";
import PlatformGovernancePage from "../../app/app/admin/platform-governance/page";

describe("Story 7.6 platform admin and KPI governance route", () => {
  it("renders policy impact and KPI alert remediation guidance", () => {
    const tree = PlatformGovernancePage();

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("Platform Administration and KPI Governance");
    expect(JSON.stringify(tree.props)).toContain("Policy impact preview");
    expect(JSON.stringify(tree.props)).toContain("Threshold breach detected");
  });
});
