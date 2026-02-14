import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

type DocumentReviewProps = {
  params: {
    caseId: string;
    documentId: string;
  };
};

export default function DocumentExtractionReviewPage({ params }: DocumentReviewProps) {
  return (
    <main>
      <h1>Document and Extraction Review</h1>
      <p className="meta">
        Story 4.6 route for document versioning, classification, extraction candidates, and review actions.
      </p>

      <div className="grid two">
        <section className="panel">
          <h2>Document Context</h2>
          <p className="meta">
            Case #{params.caseId} | Document #{params.documentId}
          </p>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <Button label="Upload New Version" variant="secondary" />
            <Button label="Update Classification" />
          </div>
        </section>

        <section className="panel">
          <h2>Extraction Candidates</h2>
          <p className="meta">Confidence-gated candidates require reviewer decision before apply.</p>
          <StatusBanner kind="warn" text="Low-confidence fields detected. Manual confirmation required." />
          <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
            <Button label="Apply Approved Fields" />
            <Button label="Reject and Re-run" variant="secondary" />
          </div>
        </section>
      </div>
    </main>
  );
}
