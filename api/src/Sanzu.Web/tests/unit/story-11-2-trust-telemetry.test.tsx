import { expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import TrustTelemetryPage from "../../app/app/governance/telemetry/page";

test("Trust telemetry page renders key sections", () => {
  const html = renderToStaticMarkup(<TrustTelemetryPage />);
  expect(html).toContain("Trust Telemetry");
  expect(html).toContain("Key Metrics");
  expect(html).toContain("Blocked by Reason");
  expect(html).toContain("Audit Event Summary");
});

test("Trust telemetry page renders metric placeholders", () => {
  const html = renderToStaticMarkup(<TrustTelemetryPage />);
  expect(html).toContain("Cases Created");
  expect(html).toContain("Cases Closed");
  expect(html).toContain("Tasks Blocked");
  expect(html).toContain("Tasks Completed");
  expect(html).toContain("Playbooks Applied");
  expect(html).toContain("Documents Uploaded");
});
