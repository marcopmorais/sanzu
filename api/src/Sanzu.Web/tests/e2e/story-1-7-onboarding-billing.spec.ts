import { test, expect } from "@playwright/test";

test("Story 1.7 route smoke", async ({ page }) => {
  await page.goto("/app/onboarding");
  await expect(page.getByRole("heading", { name: "Onboarding Workspace" })).toBeVisible();
});
