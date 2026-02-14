import { test, expect } from "@playwright/test";

test("Story 7.6 route smoke", async ({ page }) => {
  await page.goto("/app/admin/platform-governance");
  await expect(page.getByRole("heading", { name: "Platform Administration and KPI Governance" })).toBeVisible();
  await expect(page.getByRole("table", { name: "Policy impact preview" })).toBeVisible();
  await expect(page.getByText("Threshold breach detected on SLA KPI. Remediation action recommended.")).toBeVisible();
});
