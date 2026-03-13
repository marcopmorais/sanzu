import { expect, test, describe, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

// Mock the admin API module before importing the page
vi.mock("@/lib/api-client/generated/admin", () => ({
  searchAuditEvents: vi.fn().mockResolvedValue({
    items: [],
    nextCursor: undefined,
    totalCount: 0,
  }),
  exportAuditEvents: vi.fn().mockResolvedValue(undefined),
}));

describe("AuditEventViewerPage", () => {
  test("page module exports default component", async () => {
    const mod = await import("../../app/[locale]/app/admin/audit/page");
    expect(mod.default).toBeDefined();
    expect(typeof mod.default).toBe("function");
  });

  test("renders page title", async () => {
    const mod = await import("../../app/[locale]/app/admin/audit/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Audit Event Viewer");
  });

  test("renders filter controls", async () => {
    const mod = await import("../../app/[locale]/app/admin/audit/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Event Type");
    expect(html).toContain("Actor");
    expect(html).toContain("Case ID");
    expect(html).toContain("From");
    expect(html).toContain("To");
  });

  test("renders export buttons", async () => {
    const mod = await import("../../app/[locale]/app/admin/audit/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Export CSV");
    expect(html).toContain("Export JSON");
  });

  test("renders table headers", async () => {
    const mod = await import("../../app/[locale]/app/admin/audit/page");
    const Page = mod.default;
    const html = renderToStaticMarkup(<Page />);
    expect(html).toContain("Timestamp");
    expect(html).toContain("Event Type");
    expect(html).toContain("Metadata");
  });
});
