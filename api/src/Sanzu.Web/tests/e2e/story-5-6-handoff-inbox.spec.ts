import { test, expect } from "@playwright/test";

test("Story 5.6 route smoke", async ({ page }) => {
  await page.goto("/app/cases/00000000-0000-0000-0000-000000000001/handoffs/inbox");
  await expect(page.getByRole("heading", { name: "External Handoff and Process Inbox" })).toBeVisible();
});
