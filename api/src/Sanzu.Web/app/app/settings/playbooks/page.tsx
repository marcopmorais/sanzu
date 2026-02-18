import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function PlaybooksListPage() {
  return (
    <main>
      <h1>Agency Playbooks</h1>
      <p className="meta">
        Define, version, and activate playbooks so new cases follow consistent handling patterns.
      </p>

      <div className="actions" style={{ marginTop: 14 }}>
        <a href="/app/settings/playbooks/new">
          <Button label="Create Playbook" />
        </a>
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>All Playbooks</h2>
        <StatusBanner kind="ok" text="Playbooks are loaded from your agency configuration. The active playbook applies to all new cases." />
        <table className="table" aria-label="Playbooks list" style={{ marginTop: 10 }}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Version</th>
              <th>Status</th>
              <th>Updated</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={5} className="meta">
                No playbooks yet. Create your first playbook to get started.
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  );
}
