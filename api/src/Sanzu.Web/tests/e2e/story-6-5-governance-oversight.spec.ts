import { test, expect } from "@playwright/test";

test("Story 6.5 route smoke", async ({ page }) => {
  await page.goto("/app/governance/oversight");
  await expect(page.getByRole("heading", { name: "Governance and Operational Oversight" })).toBeVisible();
});
