import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

interface PlaybookDetailPageProps {
  params: Promise<{ playbookId: string }>;
}

export default async function PlaybookDetailPage({ params }: PlaybookDetailPageProps) {
  const { playbookId } = await params;

  return (
    <main>
      <h1>Playbook Detail</h1>
      <p className="meta">
        View, edit, or activate this playbook version. Only Draft or InReview playbooks can be edited.
      </p>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Playbook Information</h2>
        <StatusBanner kind="ok" text={`Viewing playbook ${playbookId}. Data will be loaded from the API.`} />

        <div className="grid two" style={{ marginTop: 14 }}>
          <div>
            <p className="meta">Name</p>
            <p>—</p>
          </div>
          <div>
            <p className="meta">Version</p>
            <p>—</p>
          </div>
          <div>
            <p className="meta">Status</p>
            <p>—</p>
          </div>
          <div>
            <p className="meta">Last Updated</p>
            <p>—</p>
          </div>
        </div>

        <div style={{ marginTop: 14 }}>
          <p className="meta">Description</p>
          <p>—</p>
        </div>

        <div style={{ marginTop: 14 }}>
          <p className="meta">Change Notes</p>
          <p>—</p>
        </div>
      </section>

      <div className="actions" style={{ marginTop: 14 }}>
        <Button label="Activate Playbook" />
        <Button label="Edit Playbook" variant="secondary" />
        <a href="/app/settings/playbooks">
          <Button label="Back to List" variant="secondary" />
        </a>
      </div>
    </main>
  );
}
