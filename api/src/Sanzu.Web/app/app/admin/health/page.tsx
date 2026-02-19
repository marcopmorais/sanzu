"use client";

import { useEffect, useState } from "react";
import {
  getHealthScores,
  triggerHealthScoreCompute,
  type TenantHealthScoreResponse,
} from "@/lib/api-client/generated/admin";
import { Button } from "@/components/atoms/Button";

const BAND_COLORS: Record<string, string> = {
  Green: "var(--green, #2a7)",
  Yellow: "var(--yellow, #c90)",
  Red: "var(--red, #c00)",
};

export default function HealthScoresPage() {
  const [scores, setScores] = useState<TenantHealthScoreResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [computing, setComputing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadScores() {
    try {
      const data = await getHealthScores();
      setScores(data);
      setLoading(false);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load health scores"
      );
      setLoading(false);
    }
  }

  useEffect(() => {
    loadScores();
  }, []);

  async function handleCompute() {
    setComputing(true);
    try {
      await triggerHealthScoreCompute();
      await loadScores();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to compute health scores"
      );
    } finally {
      setComputing(false);
    }
  }

  if (loading) {
    return (
      <main>
        <p className="meta">Loading health scores...</p>
      </main>
    );
  }

  return (
    <main>
      <h1>Tenant Health Scores</h1>
      <p className="meta">
        Health scores computed from billing, case completion, and onboarding
        signals.
      </p>

      {error && (
        <p className="meta" style={{ color: "var(--red, #c00)" }}>
          {error}
        </p>
      )}

      <div className="actions" style={{ marginTop: 14 }}>
        <Button
          label={computing ? "Computing..." : "Compute Now"}
          variant="primary"
          onClick={handleCompute}
        />
      </div>

      <section className="panel" style={{ marginTop: 14 }}>
        <table className="table" aria-label="Tenant health scores">
          <thead>
            <tr>
              <th>Tenant</th>
              <th>Overall</th>
              <th>Band</th>
              <th>Billing</th>
              <th>Cases</th>
              <th>Onboarding</th>
              <th>Issue</th>
              <th>Computed</th>
            </tr>
          </thead>
          <tbody>
            {scores.map((s) => (
              <tr key={s.id}>
                <td>{s.tenantName}</td>
                <td>
                  <strong>{s.overallScore}</strong>
                </td>
                <td>
                  <span
                    className="admin-role-badge"
                    style={{ color: BAND_COLORS[s.healthBand] || "inherit" }}
                  >
                    {s.healthBand}
                  </span>
                </td>
                <td>{s.billingScore}</td>
                <td>{s.caseCompletionScore}</td>
                <td>{s.onboardingScore}</td>
                <td>{s.primaryIssue || "—"}</td>
                <td>{new Date(s.computedAt).toLocaleString()}</td>
              </tr>
            ))}
            {scores.length === 0 && (
              <tr>
                <td colSpan={8} style={{ textAlign: "center" }}>
                  No health scores computed yet. Click &quot;Compute Now&quot; to
                  generate.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </section>
    </main>
  );
}
