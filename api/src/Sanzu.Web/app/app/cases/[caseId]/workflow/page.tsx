import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

type WorkflowPageProps = {
  params: {
    caseId: string;
  };
};

export default function CaseWorkflowPage({ params }: WorkflowPageProps) {
  return (
    <main>
      <h1>Intake, Plan, and Task Progress</h1>
      <p className="meta">Story 3.6 route for intake submission, plan generation, readiness, and task progression.</p>
      <div className="hero">
        <h2>Case #{params.caseId} readiness: 72%</h2>
        <p className="meta">Next best action is highlighted when blockers prevent task progression.</p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Intake</h2>
          <p className="meta">Case #{params.caseId} intake sections with completeness validation.</p>
          <div className="actions">
            <Button label="Save Intake" variant="secondary" />
            <Button label="Submit Intake" />
          </div>
          <p className="meta" style={{ marginTop: 8 }}>
            Validation: household profile, legal documents, and advisor contact are required to submit.
          </p>
        </section>

        <section className="panel">
          <h2>Plan + Readiness</h2>
          <p className="meta">Generate dependency-aware plan and recompute readiness.</p>
          <div className="actions">
            <Button label="Generate Plan" />
            <Button label="Recalculate Readiness" variant="secondary" />
          </div>
          <div className="kpi-grid" style={{ marginTop: 10 }}>
            <div className="kpi-card">
              <span className="meta">Open blockers</span>
              <strong>2</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Tasks due in 48h</span>
              <strong>4</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Timeline variance</span>
              <strong>+1 day</strong>
            </div>
          </div>
        </section>
      </div>

      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Task Queue and Timeline</h2>
        <p className="meta">Prioritized tasks with due-date urgency and timeline context.</p>
        <StatusBanner kind="warn" text="Blocked: Missing dependency document. Next best action highlighted." />
        <ul aria-label="Workflow next actions" className="list-tight">
          <li>Collect missing dependency document</li>
          <li>Re-run readiness calculation</li>
          <li>Advance first unblocked task</li>
        </ul>
        <p className="meta">Notification signal: owner ping sent 15 minutes ago, SLA countdown continues.</p>
      </div>
    </main>
  );
}
