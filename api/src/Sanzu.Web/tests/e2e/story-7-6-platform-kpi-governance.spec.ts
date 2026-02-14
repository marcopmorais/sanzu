import { test, expect } from "@playwright/test";

test("Story 7.6 route smoke", async ({ page }) => {
  await page.goto("/app/admin/platform-governance");
  await expect(page.getByRole("heading", { name: "Platform Administration and KPI Governance" })).toBeVisible();
});
