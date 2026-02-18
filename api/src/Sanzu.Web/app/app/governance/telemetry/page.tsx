import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function TrustTelemetryPage() {
  return (
    <main>
      <h1>Trust Telemetry</h1>
      <p className="meta">
        Aggregated compliance metrics, blocked-task breakdowns, and audit event
        summaries for pilot learning and oversight.
      </p>

      <div className="hero">
        <h2>Telemetry snapshot</h2>
        <p className="meta">
          Period: last 30 days. Metrics refresh on each page load.
        </p>
        <div className="actions">
          <Button label="Refresh" />
          <Button label="Export CSV" variant="secondary" />
        </div>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Key Metrics</h2>
          <div className="kpi-grid">
            <div className="kpi-card">
              <span className="meta">Cases Created</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Cases Closed</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Tasks Blocked</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Tasks Completed</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Playbooks Applied</span>
              <strong>—</strong>
            </div>
            <div className="kpi-card">
              <span className="meta">Documents Uploaded</span>
              <strong>—</strong>
            </div>
          </div>
        </section>

        <section className="panel">
          <h2>Blocked by Reason</h2>
          <p className="meta">
            Distribution of blocked workflow steps by canonical reason category.
          </p>
          <StatusBanner
            kind="ok"
            text="No blocked tasks in the selected period."
          />
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

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Audit Event Summary</h2>
        <p className="meta">
          Event type counts across the selected period, ordered by frequency.
        </p>
        <table className="table">
          <thead>
            <tr>
              <th>Event Type</th>
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
    </main>
  );
}
