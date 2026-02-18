import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function TenantDrilldownPage() {
  return (
    <main>
      <h1>Tenant Drilldown</h1>
      <p className="meta">
        Operational context for a single tenant. No family-sensitive data is
        exposed in this view.
      </p>

      <div className="hero">
        <h2>Tenant posture</h2>
        <div className="actions">
          <Button label="Back to Fleet" variant="secondary" />
          <Button label="Open Queues" />
        </div>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Metrics</h2>
          <div className="kpi-grid">
            <div className="kpi-card">
              <span className="meta">Total Cases</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Active Cases</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Closed Cases</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Blocked Tasks</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Completed Tasks</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Documents</span>
              <strong>—</strong>
            </div>
          </div>
        </section>

        <section className="panel">
          <h2>Blocked by Reason</h2>
          <StatusBanner kind="ok" text="No blocked tasks for this tenant." />
          <table className="table" style={{ marginTop: 10 }}>
            <thead>
              <tr>
                <th>Reason</th>
                <th>Count</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={2} className="meta">
                  Data loads from API at runtime.
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </div>
    </main>
  );
}
