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
      <div className="hero">
        <h2>Review confidence and provenance before apply</h2>
        <p className="meta">Only authorized editor/manager roles can approve extraction decisions.</p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Document Context</h2>
          <p className="meta">
            Case #{params.caseId} | Document #{params.documentId}
          </p>
          <div className="actions">
            <Button label="Upload New Version" variant="secondary" />
            <Button label="Update Classification" />
          </div>
          <p className="meta" style={{ marginTop: 8 }}>
            Provenance: uploaded by Diego F. at 2026-02-14 08:42 UTC from secure tenant workspace.
          </p>
        </section>

        <section className="panel">
          <h2>Extraction Candidates</h2>
          <p className="meta">Confidence-gated candidates require reviewer decision before apply.</p>
          <StatusBanner kind="warn" text="Low-confidence fields detected. Manual confirmation required." />
          <table aria-label="Extraction candidate decisions" className="table" style={{ marginTop: 10 }}>
            <thead>
              <tr>
                <th>Field</th>
                <th>Confidence</th>
                <th>Source</th>
                <th>Decision</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Applicant full name</td>
                <td>
                  <span className="badge ok">96%</span>
                </td>
                <td>Passport pg.1</td>
                <td>Approve</td>
              </tr>
              <tr>
                <td>Passport number</td>
                <td>
                  <span className="badge warn">62%</span>
                </td>
                <td>Passport pg.1</td>
                <td>Review required</td>
              </tr>
              <tr>
                <td>Issue date</td>
                <td>
                  <span className="badge warn">58%</span>
                </td>
                <td>Passport pg.2</td>
                <td>Review required</td>
              </tr>
            </tbody>
          </table>
          <div className="actions" style={{ marginTop: 10 }}>
            <Button label="Apply Approved Fields" />
            <Button label="Reject and Re-run" variant="secondary" />
            <Button label="Apply (Reader Blocked)" variant="secondary" disabled />
          </div>
          <p className="meta" style={{ marginTop: 8 }}>
            Reader role sees blocked apply controls and escalation guidance to assigned manager.
          </p>
        </section>
      </div>
    </main>
  );
}
