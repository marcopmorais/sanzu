import { describe, it, expect } from "vitest";
import DocumentExtractionReviewPage from "../../app/app/cases/[caseId]/documents/[documentId]/review/page";

describe("Story 4.6 document and extraction routes", () => {
  it("renders extraction confidence and review decisions with provenance context", () => {
    const tree = DocumentExtractionReviewPage({
      params: {
        caseId: "00000000-0000-0000-0000-000000000001",
        documentId: "00000000-0000-0000-0000-000000000002"
      }
    });

    expect(tree.type).toBe("main");
    expect(JSON.stringify(tree.props)).toContain("Document and Extraction Review");
    expect(JSON.stringify(tree.props)).toContain("Extraction candidate decisions");
    expect(JSON.stringify(tree.props)).toContain("Low-confidence fields detected");
  });
});
