"use client";

import { useEffect, useState } from "react";
import {
  getFunnelData,
  getFunnelStageTenants,
  type FunnelResponse,
  type FunnelStage,
  type FunnelTenantItem,
} from "@/lib/api-client/generated/admin";

export default function FunnelAnalyticsPage() {
  const [funnel, setFunnel] = useState<FunnelResponse | null>(null);
  const [cohort, setCohort] = useState<string>("");
  const [cohortValue, setCohortValue] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [drilldownStage, setDrilldownStage] = useState<string | null>(null);
  const [drilldownTenants, setDrilldownTenants] = useState<FunnelTenantItem[]>([]);
  const [drilldownLoading, setDrilldownLoading] = useState(false);

  async function loadFunnel() {
    setLoading(true);
    setError(null);
    try {
      const data = await getFunnelData(
        cohort || undefined,
        cohortValue || undefined
      );
      setFunnel(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load funnel data");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadFunnel();
  }, []);

  async function handleDrilldown(stageName: string) {
    if (drilldownStage === stageName) {
      setDrilldownStage(null);
      return;
    }
    setDrilldownStage(stageName);
    setDrilldownLoading(true);
    try {
      const tenants = await getFunnelStageTenants(stageName);
      setDrilldownTenants(tenants);
    } catch {
      setDrilldownTenants([]);
    } finally {
      setDrilldownLoading(false);
    }
  }

  function handleFilterApply() {
    loadFunnel();
  }

  const maxCount = funnel?.stages?.[0]?.count ?? 1;

  return (
    <main>
      <h1>Funnel Analytics</h1>
      <p className="meta">Tenant acquisition funnel with cohort filtering and drill-down</p>

      <div style={{ display: "flex", gap: 10, alignItems: "flex-end", marginTop: 14 }}>
        <label className="meta">
          Cohort
          <select
            value={cohort}
            onChange={(e) => setCohort(e.target.value)}
            style={{ display: "block", padding: "4px 8px", fontSize: 13, border: "1px solid #ccc", borderRadius: 4 }}
          >
            <option value="">All time</option>
            <option value="month">Month</option>
            <option value="week">Week</option>
          </select>
        </label>
        {cohort && (
          <label className="meta">
            Value
            <input
              type="text"
              placeholder={cohort === "month" ? "2026-02" : "2026-02-01"}
              value={cohortValue}
              onChange={(e) => setCohortValue(e.target.value)}
              style={{ display: "block", padding: "4px 8px", fontSize: 13, border: "1px solid #ccc", borderRadius: 4, width: 120 }}
            />
          </label>
        )}
        <button
          onClick={handleFilterApply}
          style={{
            padding: "6px 14px", fontSize: 13, border: "1px solid #ccc",
            borderRadius: 4, background: "transparent", cursor: "pointer",
          }}
        >
          Apply
        </button>
      </div>

      {loading && <p className="meta" style={{ marginTop: 14 }}>Loading funnel data...</p>}
      {error && <p className="meta" style={{ color: "#c00", marginTop: 14 }}>{error}</p>}

      {funnel && !loading && (
        <section className="panel" style={{ marginTop: 14 }} data-testid="funnel-chart">
          <h2>Acquisition Funnel</h2>
          <div style={{ display: "flex", flexDirection: "column", gap: 6, marginTop: 10 }}>
            {funnel.stages.map((stage) => (
              <div key={stage.stageName}>
                <div
                  onClick={() => handleDrilldown(stage.stageName)}
                  style={{ display: "flex", alignItems: "center", gap: 10, cursor: "pointer" }}
                  data-testid={`funnel-stage-${stage.stageName}`}
                >
                  <span style={{ width: 160, fontSize: 13, fontWeight: 600 }}>{stage.stageName}</span>
                  <div style={{ flex: 1, position: "relative", height: 28, background: "#f0f0f0", borderRadius: 4 }}>
                    <div
                      style={{
                        width: `${maxCount > 0 ? (stage.count / maxCount) * 100 : 0}%`,
                        height: "100%",
                        background: getBarColor(stage),
                        borderRadius: 4,
                        minWidth: stage.count > 0 ? 4 : 0,
                        transition: "width 0.3s ease",
                      }}
                    />
                    <span style={{ position: "absolute", left: 8, top: 5, fontSize: 12, fontWeight: 600 }}>
                      {stage.count}
                    </span>
                  </div>
                  <span className="meta" style={{ width: 100, textAlign: "right", fontSize: 12 }}>
                    {stage.dropOffPercentage > 0 ? `↓ ${stage.dropOffPercentage}%` : "—"}
                  </span>
                </div>

                {drilldownStage === stage.stageName && (
                  <div style={{ marginLeft: 170, marginTop: 6, marginBottom: 6 }}>
                    {drilldownLoading ? (
                      <p className="meta">Loading tenants...</p>
                    ) : drilldownTenants.length === 0 ? (
                      <p className="meta">No tenants at this stage.</p>
                    ) : (
                      <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }} data-testid="drilldown-table">
                        <thead>
                          <tr>
                            <th style={{ textAlign: "left", padding: "4px 8px", borderBottom: "1px solid #e0e0e0" }}>Tenant</th>
                            <th style={{ textAlign: "left", padding: "4px 8px", borderBottom: "1px solid #e0e0e0" }}>Signup Date</th>
                            <th style={{ textAlign: "right", padding: "4px 8px", borderBottom: "1px solid #e0e0e0" }}>Days at Stage</th>
                          </tr>
                        </thead>
                        <tbody>
                          {drilldownTenants.slice(0, 50).map((t) => (
                            <tr key={t.tenantId}>
                              <td style={{ padding: "4px 8px", borderBottom: "1px solid #f0f0f0" }}>
                                <a href={`/app/admin/tenants/${t.tenantId}`} style={{ color: "#0066cc" }}>{t.name}</a>
                              </td>
                              <td style={{ padding: "4px 8px", borderBottom: "1px solid #f0f0f0" }}>
                                {new Date(t.signupDate).toLocaleDateString()}
                              </td>
                              <td style={{ padding: "4px 8px", borderBottom: "1px solid #f0f0f0", textAlign: "right" }}>
                                {t.daysAtStage}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      )}
    </main>
  );
}

function getBarColor(stage: FunnelStage): string {
  if (stage.dropOffPercentage > 50) return "#cc4444";
  if (stage.dropOffPercentage > 25) return "#cc8844";
  return "#4488cc";
}
