import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

type HandoffInboxPageProps = {
  params: {
    caseId: string;
  };
};

export default function HandoffInboxPage({ params }: HandoffInboxPageProps) {
  return (
    <main>
      <h1>External Handoff and Process Inbox</h1>
      <p className="meta">
        Story 5.6 route for handoff packet lifecycle, process alias, and case-scoped inbox visibility.
      </p>

      <div className="grid two">
        <section className="panel">
          <h2>Handoff Packet</h2>
          <p className="meta">Case #{params.caseId} handoff packet generation and state updates.</p>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <Button label="Generate Packet" />
            <Button label="Mark Sent" variant="secondary" />
          </div>
        </section>

        <section className="panel">
          <h2>Process Alias and Inbox</h2>
          <p className="meta">Alias status and process inbox messages scoped to this case.</p>
          <StatusBanner kind="warn" text="Reader role cannot update handoff state. Manager action required." />
          <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
            <Button label="Provision Alias" variant="secondary" />
            <Button label="Open Inbox Thread" />
          </div>
        </section>
      </div>
    </main>
  );
}
