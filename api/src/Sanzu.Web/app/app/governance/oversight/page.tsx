import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function GovernanceOversightPage() {
  return (
    <main>
      <h1>Governance and Operational Oversight</h1>
      <p className="meta">
        Story 6.5 route for compliance monitoring, audit visibility, defaults governance, and usage indicators.
      </p>
      <div className="hero">
        <h2>Oversight risk snapshot</h2>
        <p className="meta">2 compliance exceptions and 1 usage spike need remediation follow-up.</p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Compliance + Audit</h2>
          <p className="meta">Tenant compliance status and case-level audit trail inspection.</p>
          <div className="actions">
            <Button label="Refresh Compliance" />
            <Button label="Open Audit Trail" variant="secondary" />
          </div>
          <div style={{ marginTop: 10 }}>
            <StatusBanner kind="warn" text="Retention policy warning: 2 cases require remediation action." />
          </div>
          <ul className="list-tight" aria-label="Exception context">
            <li>Case 1089: retention lock nearing expiry.</li>
            <li>Case 1033: missing disposition tag for 9 days.</li>
          </ul>
        </section>

        <section className="panel">
          <h2>Defaults + Usage Indicators</h2>
          <p className="meta">Tenant case defaults and operational usage KPI visibility.</p>
          <div className="actions">
            <Button label="Update Case Defaults" variant="secondary" />
            <Button label="View Usage Indicators" />
          </div>
          <div className="kpi-grid" style={{ marginTop: 10 }}>
            <div className="kpi-card">
              <span className="meta">Weekly case throughput</span>
              <strong>42</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Avg. readiness to completion</span>
              <strong>11.2d</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Storage trend</span>
              <strong>+8%</strong>
            </div>
          </div>
          <p className="meta" style={{ marginTop: 10 }}>
            Alert and trend context are presented before configuration changes.
          </p>
        </section>
      </div>
    </main>
  );
}
