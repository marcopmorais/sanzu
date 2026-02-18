import { test, expect } from "@playwright/test";

test("Story 10-1 playbooks route smoke", async ({ page }) => {
  await page.goto("/app/settings/playbooks");
  await expect(page.getByRole("heading", { name: "Agency Playbooks" })).toBeVisible();
  await expect(page.getByText("Create Playbook")).toBeVisible();
  await expect(page.getByText("All Playbooks")).toBeVisible();

  await page.getByRole("link", { name: "Create Playbook" }).click();
  await expect(page.getByRole("heading", { name: "Create Playbook" })).toBeVisible();
  await expect(page.getByLabel("Name")).toBeVisible();
  await expect(page.getByLabel("Description")).toBeVisible();
  await expect(page.getByLabel("Change Notes")).toBeVisible();
});
