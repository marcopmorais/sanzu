import { test, expect } from "@playwright/test";

test("Story 3.6 route smoke", async ({ page }) => {
  await page.goto("/app/cases/00000000-0000-0000-0000-000000000001/workflow");
  await expect(page.getByRole("heading", { name: "Intake, Plan, and Task Progress" })).toBeVisible();
});
