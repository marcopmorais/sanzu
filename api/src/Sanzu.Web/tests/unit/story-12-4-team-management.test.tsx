import { expect, test } from "vitest";

test("Admin API client exports team management functions", async () => {
  const mod = await import("../../lib/api-client/generated/admin");
  expect(mod.listTeamMembers).toBeDefined();
  expect(typeof mod.listTeamMembers).toBe("function");
  expect(mod.grantAdminRole).toBeDefined();
  expect(typeof mod.grantAdminRole).toBe("function");
  expect(mod.revokeAdminRole).toBeDefined();
  expect(typeof mod.revokeAdminRole).toBe("function");
});

test("Team management page exports default component", async () => {
  const mod = await import("../../app/app/admin/team/page");
  expect(mod.default).toBeDefined();
  expect(typeof mod.default).toBe("function");
});
