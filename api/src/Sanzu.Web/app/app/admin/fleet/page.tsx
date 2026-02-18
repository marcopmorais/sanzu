import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function FleetPosturePage() {
  return (
    <main>
      <h1>Tenant Fleet Posture</h1>
      <p className="meta">
        Search and segment tenants by status, blocked tasks, and payment health.
        Drill down for operational context.
      </p>

      <div className="hero">
        <h2>Fleet overview</h2>
        <div className="kpi-grid">
          <div className="kpi-card">
            <span className="meta">Total tenants</span>
            <strong>—</strong>
          </div>
          <div className="kpi-card">
            <span className="meta">Active</span>
            <strong>—</strong>
          </div>
          <div className="kpi-card">
            <span className="meta">Onboarding</span>
            <strong>—</strong>
          </div>
          <div className="kpi-card">
            <span className="meta">Payment issue</span>
            <strong>—</strong>
          </div>
          <div className="kpi-card">
            <span className="meta">Suspended</span>
            <strong>—</strong>
          </div>
        </div>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <div className="actions">
          <Button label="Search" />
          <Button label="Filter by status" variant="secondary" />
        </div>

        <table className="table" style={{ marginTop: 10 }}>
          <thead>
            <tr>
              <th>Tenant</th>
              <th>Location</th>
              <th>Status</th>
              <th>Plan</th>
              <th>Active Cases</th>
              <th>Blocked Tasks</th>
              <th>Payment Issues</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={8} className="meta">
                Data loads from API at runtime.
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  );
}
