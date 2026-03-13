import { Button } from "@/components/atoms/Button";

export default function CreatePlaybookPage() {
  return (
    <main>
      <h1>Create Playbook</h1>
      <p className="meta">
        Define a new playbook version. New playbooks start as Draft and can be activated once reviewed.
      </p>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Playbook Details</h2>
        <form>
          <div style={{ marginBottom: 12 }}>
            <label htmlFor="name">Name</label>
            <input
              id="name"
              name="name"
              type="text"
              maxLength={200}
              required
              placeholder="e.g. Standard Estate Handling v2"
              style={{ display: "block", width: "100%", marginTop: 4 }}
            />
          </div>

          <div style={{ marginBottom: 12 }}>
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              name="description"
              maxLength={2000}
              rows={4}
              placeholder="Describe the purpose and scope of this playbook..."
              style={{ display: "block", width: "100%", marginTop: 4 }}
            />
          </div>

          <div style={{ marginBottom: 12 }}>
            <label htmlFor="changeNotes">Change Notes</label>
            <textarea
              id="changeNotes"
              name="changeNotes"
              maxLength={2000}
              rows={3}
              placeholder="What changed in this version..."
              style={{ display: "block", width: "100%", marginTop: 4 }}
            />
          </div>

          <div className="actions">
            <Button label="Create Playbook" />
            <a href="/app/settings/playbooks">
              <Button label="Cancel" variant="secondary" />
            </a>
          </div>
        </form>
      </section>
    </main>
  );
}
