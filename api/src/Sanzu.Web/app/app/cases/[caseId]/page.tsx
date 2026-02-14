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
      <div className="panel grid">
        <section className="panel">
          <h2>Case #{caseId}</h2>
          <p className="meta">Current state: Active | Owner: Process Manager</p>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <Button label="Move to Review" />
            <Button label="Archive Case" variant="secondary" />
            <Button label="Invite Participant" variant="secondary" />
          </div>
          <div style={{ marginTop: 10 }}>
            <StatusBanner kind="warn" text="Reader role cannot archive this case. Escalate to manager." />
          </div>
        </section>

        <section className="panel">
          <h2>Participants</h2>
          <table aria-label="Case participants" style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr>
                <th style={{ border: "1px solid var(--line)", padding: 8, textAlign: "left" }}>Name</th>
                <th style={{ border: "1px solid var(--line)", padding: 8, textAlign: "left" }}>Role</th>
                <th style={{ border: "1px solid var(--line)", padding: 8, textAlign: "left" }}>Access</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Marina R.</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Manager</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Full</td>
              </tr>
              <tr>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Diego F.</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Editor</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Edit Allowed</td>
              </tr>
              <tr>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Jules K.</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Reader</td>
                <td style={{ border: "1px solid var(--line)", padding: 8 }}>Read Only</td>
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
