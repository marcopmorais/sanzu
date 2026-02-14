import { test, expect } from "@playwright/test";

test("Story 2.5 route smoke", async ({ page }) => {
  await page.goto("/app/cases/00000000-0000-0000-0000-000000000001");
  await expect(page.getByRole("heading", { name: "Case Lifecycle and RBAC Collaboration" })).toBeVisible();
});
