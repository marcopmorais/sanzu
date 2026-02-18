import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function CopilotDraftPage() {
  return (
    <main>
      <h1>Copilot Drafts</h1>
      <p className="meta">
        Use copilot to draft role-safe evidence requests, handoff checklists,
        recovery plans, and explanations. All drafts require your confirmation
        before any action is taken.
      </p>

      <div className="hero">
        <h2>Drafting-first copilot</h2>
        <p className="meta">
          Request Draft &rarr; Review &amp; Edit &rarr; Accept &rarr; Confirm
          Send
        </p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Evidence Request</h2>
          <p className="meta">
            Draft a message to the family requesting missing information or
            documents, based on blocked workflow steps.
          </p>
          <div className="actions">
            <Button label="Draft Evidence Request" />
          </div>
        </section>

        <section className="panel">
          <h2>Handoff Checklist</h2>
          <p className="meta">
            Generate a checklist for external partner handoffs, including
            document status and workflow progress.
          </p>
          <div className="actions">
            <Button label="Draft Handoff Checklist" variant="secondary" />
          </div>
        </section>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Draft Panel</h2>
        <p className="meta">
          Drafts appear here for review. You can edit before accepting.
        </p>
        <div className="actions">
          <Button label="Accept Draft" variant="secondary" />
          <Button label="Reject Draft" variant="secondary" />
        </div>
        <StatusBanner kind="ok" text="No pending drafts." />
      </section>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Explainability</h2>
        <p className="meta">
          Every draft includes a &ldquo;Based on&rdquo; block showing the
          reason category, confidence band, and what is missing or unknown.
        </p>
        <table className="table">
          <thead>
            <tr>
              <th>Field</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Based on</td>
              <td>The evidence or context that informed the draft</td>
            </tr>
            <tr>
              <td>Reason category</td>
              <td>Canonical reason code for the blocked state</td>
            </tr>
            <tr>
              <td>Confidence band</td>
              <td>Low, medium, or high confidence in the draft</td>
            </tr>
            <tr>
              <td>Safe fallback</td>
              <td>What to do if the draft does not resolve the issue</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  );
}
