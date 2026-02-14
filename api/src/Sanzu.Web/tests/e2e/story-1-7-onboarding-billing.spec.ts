import { test, expect } from "@playwright/test";

test("Story 1.7 route smoke", async ({ page }) => {
  await page.goto("/app/onboarding");
  await expect(page.getByRole("heading", { name: "Onboarding Workspace" })).toBeVisible();
  await expect(page.getByLabel("Agency name")).toBeVisible();
  await expect(page.getByText("At least one Process Manager invite is required.")).toBeVisible();
  await page.getByRole("link", { name: "Continue to Billing" }).click();
  await expect(page.getByRole("heading", { name: "Billing Activation" })).toBeVisible();
  await expect(page.getByRole("list", { name: "Recovery path" })).toBeVisible();
});
