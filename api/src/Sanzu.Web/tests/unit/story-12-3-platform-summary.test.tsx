import { expect, test } from "vitest";

test("Admin API client exports getPlatformSummary", async () => {
  const mod = await import("../../lib/api-client/generated/admin");
  expect(mod.getPlatformSummary).toBeDefined();
  expect(typeof mod.getPlatformSummary).toBe("function");
});

test("Platform operations summary page exports default component", async () => {
  const mod = await import("../../app/app/admin/platform/page");
  expect(mod.default).toBeDefined();
  expect(typeof mod.default).toBe("function");
});
