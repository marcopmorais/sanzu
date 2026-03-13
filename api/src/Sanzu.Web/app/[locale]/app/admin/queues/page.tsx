import { Button } from "@/components/atoms/Button";

export default function AdminQueuesPage() {
  return (
    <main>
      <h1>Mission Control Queues</h1>
      <p className="meta">
        Operational queues for diagnosis and remediation. Items are
        reason-coded for fast scanning.
      </p>

      <div className="hero">
        <h2>Queue overview</h2>
        <div className="actions">
          <Button label="Refresh" />
        </div>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <table className="table">
          <thead>
            <tr>
              <th>Queue</th>
              <th>Scope</th>
              <th>Items</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Onboarding stuck</td>
              <td>tenant</td>
              <td>—</td>
              <td><Button label="Open" variant="secondary" /></td>
            </tr>
            <tr>
              <td>Compliance exception</td>
              <td>tenant</td>
              <td>—</td>
              <td><Button label="Open" variant="secondary" /></td>
            </tr>
            <tr>
              <td>KPI threshold breach</td>
              <td>tenant</td>
              <td>—</td>
              <td><Button label="Open" variant="secondary" /></td>
            </tr>
            <tr>
              <td>Failed payment</td>
              <td>tenant</td>
              <td>—</td>
              <td><Button label="Open" variant="secondary" /></td>
            </tr>
            <tr>
              <td>Support escalation</td>
              <td>tenant</td>
              <td>—</td>
              <td><Button label="Open" variant="secondary" /></td>
            </tr>
          </tbody>
        </table>
      </section>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Event Stream Drilldown</h2>
        <p className="meta">
          Select a tenant from a queue item to view their reason-coded event
          stream. Privacy-safe summaries only.
        </p>
      </section>
    </main>
  );
}
