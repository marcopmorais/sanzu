import { test, expect } from "@playwright/test";

test("Story 4.6 route smoke", async ({ page }) => {
  await page.goto("/app/cases/00000000-0000-0000-0000-000000000001/documents/00000000-0000-0000-0000-000000000002/review");
  await expect(page.getByRole("heading", { name: "Document and Extraction Review" })).toBeVisible();
  await expect(page.getByRole("table", { name: "Extraction candidate decisions" })).toBeVisible();
  await expect(page.getByText("Low-confidence fields detected. Manual confirmation required.")).toBeVisible();
});
