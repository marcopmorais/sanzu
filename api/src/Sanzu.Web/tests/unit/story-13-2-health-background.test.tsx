import { expect, test } from "vitest";

test("Admin API client exports health score compute trigger", async () => {
  const mod = await import("../../lib/api-client/generated/admin");
  expect(mod.triggerHealthScoreCompute).toBeDefined();
  expect(typeof mod.triggerHealthScoreCompute).toBe("function");
  expect(mod.getHealthScores).toBeDefined();
  expect(typeof mod.getHealthScores).toBe("function");
});

test("Health scores page exports default component", async () => {
  const mod = await import("../../app/app/admin/health/page");
  expect(mod.default).toBeDefined();
  expect(typeof mod.default).toBe("function");
});
