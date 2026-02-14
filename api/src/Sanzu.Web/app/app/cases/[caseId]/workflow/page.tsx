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

      <div className="grid two">
        <section className="panel">
          <h2>Intake</h2>
          <p className="meta">Case #{params.caseId} intake sections with completeness validation.</p>
          <div style={{ display: "flex", gap: 8 }}>
            <Button label="Save Intake" variant="secondary" />
            <Button label="Submit Intake" />
          </div>
        </section>

        <section className="panel">
          <h2>Plan + Readiness</h2>
          <p className="meta">Generate dependency-aware plan and recompute readiness.</p>
          <div style={{ display: "flex", gap: 8 }}>
            <Button label="Generate Plan" />
            <Button label="Recalculate Readiness" variant="secondary" />
          </div>
        </section>
      </div>

      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Task Queue and Timeline</h2>
        <p className="meta">Prioritized tasks with due-date urgency and timeline context.</p>
        <StatusBanner kind="warn" text="Blocked: Missing dependency document. Next best action highlighted." />
        <ul aria-label="Workflow next actions" style={{ marginTop: 10 }}>
          <li>Collect missing dependency document</li>
          <li>Re-run readiness calculation</li>
          <li>Advance first unblocked task</li>
        </ul>
      </div>
    </main>
  );
}
