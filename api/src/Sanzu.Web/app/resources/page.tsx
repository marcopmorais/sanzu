export default function ResourcesPage() {
  return (
    <main>
      <h1>Resources</h1>
      <p className="meta">Guides and reference content for onboarding and operational success.</p>
      <div className="panel">
        <table className="table" aria-label="Resource library">
          <thead>
            <tr>
              <th>Resource</th>
              <th>Audience</th>
              <th>Format</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Onboarding runbook</td>
              <td>Agency administrators</td>
              <td>Guide</td>
            </tr>
            <tr>
              <td>Compliance retention checklist</td>
              <td>Operations leaders</td>
              <td>Checklist</td>
            </tr>
            <tr>
              <td>Workflow dependency primer</td>
              <td>Process managers</td>
              <td>Playbook</td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  );
}
