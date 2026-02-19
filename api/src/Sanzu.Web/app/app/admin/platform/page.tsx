"use client";

import { useEffect, useState } from "react";
import { getPlatformSummary, type PlatformOperationsSummaryResponse } from "@/lib/api-client/generated/admin";

export default function PlatformOperationsSummaryPage() {
  const [summary, setSummary] = useState<PlatformOperationsSummaryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const data = await getPlatformSummary();
        if (!cancelled) {
          setSummary(data);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load platform summary");
          setLoading(false);
        }
      }
    }

    load();
    return () => { cancelled = true; };
  }, []);

  if (loading) {
    return (
      <main>
        <p className="meta">Loading platform operations summary...</p>
      </main>
    );
  }

  if (error || !summary) {
    return (
      <main>
        <h1>Platform Operations Summary</h1>
        <p className="meta">{error ?? "Unable to load summary data."}</p>
      </main>
    );
  }

  return (
    <main>
      <h1>Platform Operations Summary</h1>
      <p className="meta">At-a-glance view of platform-wide activity and resource usage.</p>

      <div className="grid two" style={{ marginTop: 14 }}>
        <section className="panel">
          <h2>Tenants</h2>
          <p className="meta">Active tenant organizations on the platform.</p>
          <p style={{ fontSize: 32, fontWeight: 700 }}>{summary.totalActiveTenants}</p>
        </section>

        <section className="panel">
          <h2>Active Cases</h2>
          <p className="meta">Cases currently open across all tenants.</p>
          <p style={{ fontSize: 32, fontWeight: 700 }}>{summary.totalActiveCases}</p>
        </section>

        <section className="panel">
          <h2>Workflow Steps</h2>
          <p className="meta">Step status distribution across active cases.</p>
          <table className="table" aria-label="Workflow step status breakdown">
            <thead>
              <tr>
                <th>Status</th>
                <th>Count</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Completed</td>
                <td>{summary.workflowStepsCompleted}</td>
              </tr>
              <tr>
                <td>Active</td>
                <td>{summary.workflowStepsActive}</td>
              </tr>
              <tr>
                <td>Blocked</td>
                <td>{summary.workflowStepsBlocked}</td>
              </tr>
            </tbody>
          </table>
        </section>

        <section className="panel">
          <h2>Documents</h2>
          <p className="meta">Total documents stored across all cases.</p>
          <p style={{ fontSize: 32, fontWeight: 700 }}>{summary.totalDocuments}</p>
        </section>
      </div>
    </main>
  );
}
