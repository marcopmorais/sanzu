import { describe, it, expect } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import PlaybooksListPage from "../../app/app/settings/playbooks/page";
import CreatePlaybookPage from "../../app/app/settings/playbooks/new/page";

describe("Story 10-1 agency playbooks routes", () => {
  it("renders playbooks list page with expected structure", () => {
    const html = renderToStaticMarkup(createElement(PlaybooksListPage));

    expect(html).toContain("Agency Playbooks");
    expect(html).toContain("Create Playbook");
    expect(html).toContain("All Playbooks");
    expect(html).toContain("No playbooks yet");
  });

  it("renders create playbook page with form fields", () => {
    const html = renderToStaticMarkup(createElement(CreatePlaybookPage));

    expect(html).toContain("Create Playbook");
    expect(html).toContain("Playbook Details");
    expect(html).toContain("Name");
    expect(html).toContain("Description");
    expect(html).toContain("Change Notes");
    expect(html).toContain("Cancel");
  });
});
