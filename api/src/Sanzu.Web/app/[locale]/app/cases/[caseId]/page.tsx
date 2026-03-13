import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

type CasePageProps = {
  params: {
    caseId: string;
  };
};

export default function CaseLifecyclePage({ params }: CasePageProps) {
  const { caseId } = params;

  return (
    <main>
      <h1>Case Lifecycle and RBAC Collaboration</h1>
      <p className="meta">Story 2.5 route for lifecycle transitions and role-scoped participant actions.</p>
      <div className="hero">
        <h2>Ownership context: Process Manager Marina R.</h2>
        <p className="meta">Lifecycle changes are role-gated and logged to the case milestone timeline.</p>
      </div>
      <div className="panel grid">
        <section className="panel">
          <h2>Case #{caseId}</h2>
          <p className="meta">Current state: Active | Owner: Process Manager | Last transition: Intake Completed</p>
          <div className="actions">
            <Button label="Move to Review" />
            <Button label="Archive Case" variant="secondary" />
            <Button label="Invite Participant" variant="secondary" />
          </div>
          <div style={{ marginTop: 10 }}>
            <StatusBanner kind="warn" text="Reader role cannot archive this case. Escalate to manager." />
          </div>
          <ul className="list-tight" aria-label="Lifecycle guidance">
            <li>Transition guard: blocked if required tasks are incomplete.</li>
            <li>Confirmation modal required before archive transition is committed.</li>
          </ul>
        </section>

        <section className="panel">
          <h2>Participants</h2>
          <table aria-label="Case participants" className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Access</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Marina R.</td>
                <td>
                  <span className="badge ok">Manager</span>
                </td>
                <td>Full</td>
              </tr>
              <tr>
                <td>Diego F.</td>
                <td>
                  <span className="badge info">Editor</span>
                </td>
                <td>Edit Allowed</td>
              </tr>
              <tr>
                <td>Jules K.</td>
                <td>
                  <span className="badge warn">Reader</span>
                </td>
                <td>Read Only</td>
              </tr>
            </tbody>
          </table>
          <p className="meta" style={{ marginTop: 8 }}>
            Empty-state behavior: when no participants are present, show invitation CTA and role guidance.
          </p>
        </section>
      </div>
    </main>
  );
}
