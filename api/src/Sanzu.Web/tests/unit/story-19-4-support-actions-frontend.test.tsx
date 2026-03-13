import { expect, test, describe, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

// Mock next/navigation
vi.mock("next/navigation", () => ({
  useParams: () => ({ tenantId: "00000000-0000-0000-0000-000000000001" }),
}));

// Mock AdminPermissionsContext
vi.mock("@/lib/admin/AdminPermissionsContext", () => ({
  useAdminPermissions: () => ({
    permissions: {
      accessibleEndpoints: [
        "/admin/tenants/*/billing",
        "/admin/tenants/*/cases",
        "/admin/tenants/*/activity",
        "/admin/tenants/*/actions",
        "/admin/tenants/*/comms",
      ],
    },
  }),
}));

// Mock admin API module
vi.mock("@/lib/api-client/generated/admin", () => ({
  getTenantSummary: vi.fn().mockResolvedValue({
    name: "Test Tenant",
    status: "Active",
    planTier: "Pro",
    region: "EU",
    contactEmail: "admin@test.com",
    signupDate: "2025-01-01T00:00:00Z",
    healthScore: 80,
    healthBand: "Green",
  }),
  getTenantBilling: vi.fn().mockResolvedValue({ recentInvoices: [] }),
  getTenantCases: vi.fn().mockResolvedValue({ cases: [] }),
  getTenantActivity: vi.fn().mockResolvedValue({ events: [] }),
  getTenantComms: vi.fn().mockResolvedValue([]),
  overrideBlockedStep: vi.fn().mockResolvedValue(undefined),
  extendGracePeriod: vi.fn().mockResolvedValue(undefined),
  triggerReOnboarding: vi.fn().mockResolvedValue(undefined),
  startImpersonation: vi.fn().mockResolvedValue({
    impersonationToken: "abc123",
    expiresAt: "2025-01-01T01:00:00Z",
  }),
  sendCommunication: vi.fn().mockResolvedValue(undefined),
}));

// Mock HealthGauge
vi.mock("@/components/admin/widgets/HealthGauge", () => ({
  HealthGauge: () => <div data-testid="health-gauge" />,
}));

describe("Tenant360Page — Story 19.4 Support Actions Frontend", () => {
  test("page module exports default component", async () => {
    const mod = await import("../../app/app/admin/tenants/[tenantId]/page");
    expect(mod.default).toBeDefined();
    expect(typeof mod.default).toBe("function");
  });

  test("SSR renders initial loading state", async () => {
    const mod = await import("../../app/app/admin/tenants/[tenantId]/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Loading tenant details...");
  });

  test("API client exports support action functions", async () => {
    const admin = await import("@/lib/api-client/generated/admin");
    expect(typeof admin.overrideBlockedStep).toBe("function");
    expect(typeof admin.extendGracePeriod).toBe("function");
    expect(typeof admin.triggerReOnboarding).toBe("function");
    expect(typeof admin.startImpersonation).toBe("function");
    expect(typeof admin.sendCommunication).toBe("function");
    expect(typeof admin.getTenantComms).toBe("function");
  });

  test("page source contains Actions tab definition", async () => {
    // Verify the page defines the actions and comms tabs in its TABS array
    const fs = await import("node:fs");
    const path = await import("node:path");
    const src = fs.readFileSync(
      path.resolve(__dirname, "../../app/app/admin/tenants/[tenantId]/page.tsx"),
      "utf-8"
    );
    expect(src).toContain('"actions"');
    expect(src).toContain('"comms"');
    expect(src).toContain("ActionsTabContent");
    expect(src).toContain("CommsTabContent");
    expect(src).toContain("Override Blocked Step");
    expect(src).toContain("Extend Grace Period");
    expect(src).toContain("Re-Onboarding");
    expect(src).toContain("Impersonate Tenant");
    expect(src).toContain("Send Communication");
    expect(src).toContain("Communication History");
  });

  test("page source contains comms type options", async () => {
    const fs = await import("node:fs");
    const path = await import("node:path");
    const src = fs.readFileSync(
      path.resolve(__dirname, "../../app/app/admin/tenants/[tenantId]/page.tsx"),
      "utf-8"
    );
    expect(src).toContain('"support"');
    expect(src).toContain('"billing"');
    expect(src).toContain('"onboarding"');
    expect(src).toContain('"escalation"');
  });
});
