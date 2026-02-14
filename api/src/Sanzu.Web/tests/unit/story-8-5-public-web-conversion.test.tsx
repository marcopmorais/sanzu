import { describe, it, expect, vi } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import HomePage from "../../app/page";
import DemoPage from "../../app/demo/page";
import StartPage from "../../app/start/page";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() })
}));

describe("Story 8.5 public web conversion routes", () => {
  it("renders public trust-to-conversion journey copy and legal cues", () => {
    const homeHtml = renderToStaticMarkup(createElement(HomePage));
    const demoHtml = renderToStaticMarkup(createElement(DemoPage));
    const startHtml = renderToStaticMarkup(createElement(StartPage));

    expect(homeHtml).toContain("Sanzu");
    expect(homeHtml).toContain("Privacy");

    expect(demoHtml).toContain("Request a Demo");
    expect(demoHtml).toContain("Privacy Policy");

    expect(startHtml).toContain("Start with Sanzu");
    expect(startHtml).toContain("Terms of Service");
  });
});
