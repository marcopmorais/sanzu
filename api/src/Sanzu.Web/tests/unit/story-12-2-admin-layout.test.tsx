import { expect, test } from "vitest";

test("AdminPermissionsContext module exports expected functions", async () => {
  const mod = await import("../../lib/admin/AdminPermissionsContext");
  expect(mod.AdminPermissionsProvider).toBeDefined();
  expect(mod.useAdminPermissions).toBeDefined();
});

test("Admin API client exports getAdminPermissions", async () => {
  const mod = await import("../../lib/api-client/generated/admin");
  expect(mod.getAdminPermissions).toBeDefined();
});

test("Admin layout module exports default component", async () => {
  const mod = await import("../../app/app/admin/layout");
  expect(mod.default).toBeDefined();
  expect(typeof mod.default).toBe("function");
});
