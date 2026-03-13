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
      <div className="hero">
        <h2>Case #{params.caseId} handoff status: awaiting advisor acknowledgement</h2>
        <p className="meta">Every handoff transition captures timestamp, actor, and channel metadata.</p>
      </div>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Handoff Packet</h2>
          <p className="meta">Case #{params.caseId} handoff packet generation and state updates.</p>
          <div className="actions">
            <Button label="Generate Packet" />
            <Button label="Mark Sent" variant="secondary" />
          </div>
          <ul className="list-tight" aria-label="Handoff metadata">
            <li>Packet version: v3 (includes policy disclosures + intake summary)</li>
            <li>Last outbound timestamp: 2026-02-14 16:25 UTC</li>
            <li>Current assignee: Advisory Team North</li>
          </ul>
        </section>

        <section className="panel">
          <h2>Process Alias and Inbox</h2>
          <p className="meta">Alias status and process inbox messages scoped to this case.</p>
          <StatusBanner kind="warn" text="Reader role cannot update handoff state. Manager action required." />
          <div style={{ marginTop: 10 }} className="actions">
            <Button label="Provision Alias" variant="secondary" />
            <Button label="Open Inbox Thread" />
            <Button label="Mark Complete (Blocked)" variant="secondary" disabled />
          </div>
          <table aria-label="Inbox thread metadata" className="table" style={{ marginTop: 10 }}>
            <thead>
              <tr>
                <th>Thread</th>
                <th>Last message</th>
                <th>SLA</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Advisor Intake Clarification</td>
                <td>7m ago by Sofia M.</td>
                <td>
                  <span className="badge info">14h remaining</span>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </div>
    </main>
  );
}
