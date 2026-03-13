import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function RecoveryPlanPage() {
  return (
    <main>
      <h1>Recovery Plans</h1>
      <p className="meta">
        Generate recovery plans for blocked cases using reason categories and
        event evidence. Copilot explains &ldquo;why&rdquo; and drafts
        step-by-step resolution plans.
      </p>

      <div className="hero">
        <h2>Explain and recover</h2>
        <p className="meta">
          Detect blocked state &rarr; Explain why &rarr; Draft plan &rarr;
          Resolve
        </p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Recovery Plan</h2>
          <p className="meta">
            Select a blocked case or workflow step to generate a recovery plan
            with evidence checklist and escalation path.
          </p>
          <div className="actions">
            <Button label="Generate Recovery Plan" />
          </div>
          <StatusBanner kind="ok" text="No blocked cases require recovery." />
        </section>

        <section className="panel">
          <h2>Explain Why</h2>
          <p className="meta">
            Get a plain-language explanation of why a case or step is blocked,
            based on reason categories and safe event anchors.
          </p>
          <div className="actions">
            <Button label="Explain Blocked State" variant="secondary" />
          </div>
        </section>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Copilot Boundaries</h2>
        <p className="meta">
          Copilot cannot perform autonomous changes to case lifecycle, tenant
          policies, or user roles. All actions require explicit confirmation.
        </p>
        <table className="table">
          <thead>
            <tr>
              <th>Action</th>
              <th>Allowed</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Draft recovery plans</td>
              <td>Yes</td>
            </tr>
            <tr>
              <td>Explain blocked states</td>
              <td>Yes</td>
            </tr>
            <tr>
              <td>Change case lifecycle</td>
              <td>No (requires user action)</td>
            </tr>
            <tr>
              <td>Modify tenant policies</td>
              <td>No (requires admin action)</td>
            </tr>
            <tr>
              <td>Change user roles</td>
              <td>No (requires admin action)</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  );
}
