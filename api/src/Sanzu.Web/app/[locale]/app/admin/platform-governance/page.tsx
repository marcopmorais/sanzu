import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function PlatformGovernancePage() {
  return (
    <main>
      <h1>Platform Administration and KPI Governance</h1>
      <p className="meta">
        Story 7.6 route for tenant admin controls, support diagnostics, and KPI threshold governance.
      </p>
      <div className="hero">
        <h2>Platform posture: stable with one threshold breach</h2>
        <p className="meta">Apply controls with impact previews, then track KPI remediation in drilldown workflows.</p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Tenant Admin Controls</h2>
          <p className="meta">Lifecycle state updates, policy control actions, and diagnostics session management.</p>
          <div className="actions">
            <Button label="Update Tenant Lifecycle" />
            <Button label="Apply Policy Controls" variant="secondary" />
            <Button label="Start Diagnostics Session" variant="secondary" />
          </div>
          <table className="table" aria-label="Policy impact preview" style={{ marginTop: 10 }}>
            <thead>
              <tr>
                <th>Control</th>
                <th>Impact preview</th>
                <th>Approval</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Strict session timeout</td>
                <td>May terminate 3 active support sessions.</td>
                <td>Security admin required</td>
              </tr>
            </tbody>
          </table>
        </section>

        <section className="panel">
          <h2>KPI Governance</h2>
          <p className="meta">Dashboard drilldowns, threshold updates, and alert evaluation workflows.</p>
          <StatusBanner kind="warn" text="Threshold breach detected on SLA KPI. Remediation action recommended." />
          <div style={{ marginTop: 10 }} className="actions">
            <Button label="Refresh KPI Dashboard" />
            <Button label="Update Thresholds" variant="secondary" />
            <Button label="Evaluate Alerts" variant="secondary" />
          </div>
          <ul className="list-tight" aria-label="KPI drilldown guidance">
            <li>Drilldown links show affected tenants and change history.</li>
            <li>Remediation playbook is attached to every active threshold alert.</li>
          </ul>
        </section>
      </div>
    </main>
  );
}
