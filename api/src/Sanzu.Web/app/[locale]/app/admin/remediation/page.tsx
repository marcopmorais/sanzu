import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function RemediationPage() {
  return (
    <main>
      <h1>Remediation Actions</h1>
      <p className="meta">
        Apply remediation with impact preview, audit notes, and verification.
        Every action is traceable and role-separated.
      </p>

      <div className="hero">
        <h2>Remediation workflow</h2>
        <p className="meta">
          Preview &rarr; Audit Note &rarr; Commit &rarr; Verify &rarr; Resolve
        </p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Impact Preview</h2>
          <p className="meta">
            Select an action to see its impact summary and reversibility before
            committing.
          </p>
          <div className="actions">
            <Button label="Preview Action" />
          </div>
          <StatusBanner
            kind="ok"
            text="No pending remediation actions."
          />
        </section>

        <section className="panel">
          <h2>Verification</h2>
          <p className="meta">
            After committing, verify the outcome. Queue items can only be
            resolved after verification passes (or with an override note).
          </p>
          <div className="actions">
            <Button label="Start Verification" variant="secondary" />
            <Button label="Resolve" variant="secondary" />
          </div>
        </section>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Action Catalog</h2>
        <table className="table">
          <thead>
            <tr>
              <th>Action</th>
              <th>Impact</th>
              <th>Reversible</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Contact tenant</td>
              <td>Contact tenant to resolve issue.</td>
              <td>Yes</td>
            </tr>
            <tr>
              <td>Extend grace period</td>
              <td>Extend payment grace period for tenant.</td>
              <td>Yes</td>
            </tr>
            <tr>
              <td>Suspend tenant</td>
              <td>Suspend tenant access.</td>
              <td>No</td>
            </tr>
            <tr>
              <td>Escalate to support</td>
              <td>Escalate to support engineering.</td>
              <td>Yes</td>
            </tr>
            <tr>
              <td>Run diagnostics</td>
              <td>Run least-privilege diagnostics.</td>
              <td>Yes</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  );
}
