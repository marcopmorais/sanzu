import { test, expect } from "@playwright/test";

test("Story 8.5 public routes smoke", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Sanzu" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Privacy" })).toBeVisible();
  await page.goto("/demo");
  await expect(page.getByRole("heading", { name: "Request a Demo" })).toBeVisible();
  await expect(page.getByText("Validation: business email and consent acknowledgment are required before submit.")).toBeVisible();
  await page.goto("/start");
  await expect(page.getByRole("heading", { name: "Start with Sanzu" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Terms of Service" })).toBeVisible();
});
