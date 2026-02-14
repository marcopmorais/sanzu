import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function GovernanceOversightPage() {
  return (
    <main>
      <h1>Governance and Operational Oversight</h1>
      <p className="meta">
        Story 6.5 route for compliance monitoring, audit visibility, defaults governance, and usage indicators.
      </p>

      <div className="grid two">
        <section className="panel">
          <h2>Compliance + Audit</h2>
          <p className="meta">Tenant compliance status and case-level audit trail inspection.</p>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <Button label="Refresh Compliance" />
            <Button label="Open Audit Trail" variant="secondary" />
          </div>
          <div style={{ marginTop: 10 }}>
            <StatusBanner kind="warn" text="Retention policy warning: 2 cases require remediation action." />
          </div>
        </section>

        <section className="panel">
          <h2>Defaults + Usage Indicators</h2>
          <p className="meta">Tenant case defaults and operational usage KPI visibility.</p>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <Button label="Update Case Defaults" variant="secondary" />
            <Button label="View Usage Indicators" />
          </div>
          <p className="meta" style={{ marginTop: 10 }}>
            Alert and trend context are presented before configuration changes.
          </p>
        </section>
      </div>
    </main>
  );
}
