"use client";

import { useEffect, useState } from "react";
import {
  getRevenueOverview,
  getRevenueTrends,
  getBillingHealth,
  exportRevenueCsv,
  exportBillingHealthCsv,
  type RevenueOverview,
  type RevenueTrends,
  type BillingHealth,
} from "@/lib/api-client/generated/admin";
import { TrendLineChart } from "@/components/admin/charts/TrendLineChart";
import { PlanDistributionChart } from "@/components/admin/charts/PlanDistributionChart";

type TrendPeriod = "daily" | "weekly" | "monthly";

export default function RevenueDashboardPage() {
  const [overview, setOverview] = useState<RevenueOverview | null>(null);
  const [trends, setTrends] = useState<RevenueTrends | null>(null);
  const [health, setHealth] = useState<BillingHealth | null>(null);
  const [period, setPeriod] = useState<TrendPeriod>("monthly");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [overviewData, healthData] = await Promise.all([
          getRevenueOverview(),
          getBillingHealth(),
        ]);
        if (!cancelled) {
          setOverview(overviewData);
          setHealth(healthData);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load revenue data");
          setLoading(false);
        }
      }
    }

    load();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadTrends() {
      try {
        const trendsData = await getRevenueTrends(period);
        if (!cancelled) {
          setTrends(trendsData);
        }
      } catch {
        // Trends failure is non-critical
      }
    }

    loadTrends();
    return () => { cancelled = true; };
  }, [period]);

  if (loading) {
    return (
      <main>
        <p className="meta">Loading revenue dashboard...</p>
      </main>
    );
  }

  if (error || !overview) {
    return (
      <main>
        <h1>Revenue Dashboard</h1>
        <p className="meta" style={{ color: "var(--red, #c00)" }}>
          {error ?? "Unable to load revenue data."}
        </p>
      </main>
    );
  }

  return (
    <main>
      <h1>Revenue Dashboard</h1>
      <p className="meta">Financial health overview: MRR, trends, plan distribution, and billing status.</p>

      {/* Headline Metrics */}
      <div
        style={{
          display: "grid",
          gap: 14,
          gridTemplateColumns: "repeat(4, minmax(0, 1fr))",
          marginTop: 14,
        }}
      >
        <MetricCard label="MRR" value={`€${overview.mrr.toLocaleString()}`} />
        <MetricCard label="ARR" value={`€${overview.arr.toLocaleString()}`} />
        <MetricCard label="Churn Rate" value={`${overview.churnRate}%`} warn={overview.churnRate > 5} />
        <MetricCard label="Growth Rate" value={`${overview.growthRate}%`} positive={overview.growthRate > 0} />
      </div>

      {/* Trend Chart */}
      <section style={{ marginTop: 24 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 8 }}>
          <h2 style={{ margin: 0 }}>MRR Trends</h2>
          <div style={{ display: "flex", gap: 4 }}>
            {(["daily", "weekly", "monthly"] as TrendPeriod[]).map((p) => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                style={{
                  padding: "4px 10px",
                  fontSize: 12,
                  border: "1px solid var(--border, #ccc)",
                  borderRadius: 4,
                  background: p === period ? "var(--primary, #2563eb)" : "transparent",
                  color: p === period ? "#fff" : "inherit",
                  cursor: "pointer",
                }}
                aria-pressed={p === period}
              >
                {p.charAt(0).toUpperCase() + p.slice(1)}
              </button>
            ))}
          </div>
        </div>
        {trends ? (
          <TrendLineChart data={trends.dataPoints} />
        ) : (
          <p className="meta">Loading trends...</p>
        )}
      </section>

      {/* Plan Distribution */}
      <section style={{ marginTop: 24 }}>
        <h2>Revenue by Plan</h2>
        <PlanDistributionChart data={overview.planBreakdown} />
      </section>

      {/* Billing Health */}
      {health && (
        <section style={{ marginTop: 24 }}>
          <h2>Billing Health</h2>
          <div style={{ display: "flex", gap: 14, marginBottom: 12 }}>
            <span className="meta">Failed payments: {health.failedPaymentCount}</span>
            <span className="meta">Grace period: {health.gracePeriodCount}</span>
            <span className="meta">Upcoming renewals: {health.upcomingRenewals.length}</span>
          </div>

          {health.failedPayments.length > 0 && (
            <>
              <h3>Failed Payments</h3>
              <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
                <thead>
                  <tr>
                    <th style={thStyle}>Tenant</th>
                    <th style={thStyle}>Amount</th>
                    <th style={thStyle}>Last Failed</th>
                  </tr>
                </thead>
                <tbody>
                  {health.failedPayments.map((item) => (
                    <tr key={item.tenantId}>
                      <td style={tdStyle}>
                        <a href={`/app/admin/tenants/${item.tenantId}`}>{item.tenantName}</a>
                      </td>
                      <td style={tdStyle}>€{item.failedAmount?.toLocaleString() ?? "—"}</td>
                      <td style={tdStyle}>{item.lastFailedAt ? new Date(item.lastFailedAt).toLocaleDateString() : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {health.gracePeriodTenants.length > 0 && (
            <>
              <h3>Grace Period</h3>
              <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
                <thead>
                  <tr>
                    <th style={thStyle}>Tenant</th>
                    <th style={thStyle}>Retry At</th>
                  </tr>
                </thead>
                <tbody>
                  {health.gracePeriodTenants.map((item) => (
                    <tr key={item.tenantId}>
                      <td style={tdStyle}>
                        <a href={`/app/admin/tenants/${item.tenantId}`}>{item.tenantName}</a>
                      </td>
                      <td style={tdStyle}>{item.gracePeriodRetryAt ? new Date(item.gracePeriodRetryAt).toLocaleDateString() : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {health.upcomingRenewals.length > 0 && (
            <>
              <h3>Upcoming Renewals</h3>
              <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
                <thead>
                  <tr>
                    <th style={thStyle}>Tenant</th>
                    <th style={thStyle}>Renewal Date</th>
                  </tr>
                </thead>
                <tbody>
                  {health.upcomingRenewals.map((item) => (
                    <tr key={item.tenantId}>
                      <td style={tdStyle}>
                        <a href={`/app/admin/tenants/${item.tenantId}`}>{item.tenantName}</a>
                      </td>
                      <td style={tdStyle}>{item.nextRenewalDate ? new Date(item.nextRenewalDate).toLocaleDateString() : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </section>
      )}

      {/* Export Buttons */}
      <section style={{ marginTop: 24, display: "flex", gap: 12 }}>
        <button
          onClick={() => exportRevenueCsv()}
          style={exportBtnStyle}
        >
          Export Revenue CSV
        </button>
        <button
          onClick={() => exportBillingHealthCsv()}
          style={exportBtnStyle}
        >
          Export Billing Health CSV
        </button>
      </section>
    </main>
  );
}

function MetricCard({ label, value, warn, positive }: { label: string; value: string; warn?: boolean; positive?: boolean }) {
  const color = warn ? "var(--red, #c00)" : positive ? "var(--green, #059669)" : undefined;
  return (
    <div
      style={{
        padding: 14,
        border: "1px solid var(--border, #e5e7eb)",
        borderRadius: 8,
        textAlign: "center",
      }}
    >
      <p className="meta" style={{ margin: 0 }}>{label}</p>
      <p style={{ fontSize: 24, fontWeight: 700, margin: "4px 0 0", color }}>{value}</p>
    </div>
  );
}

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "8px 12px",
  borderBottom: "2px solid var(--border, #e5e7eb)",
  fontWeight: 600,
};

const tdStyle: React.CSSProperties = {
  padding: "8px 12px",
  borderBottom: "1px solid var(--border, #e5e7eb)",
};

const exportBtnStyle: React.CSSProperties = {
  padding: "8px 16px",
  border: "1px solid var(--border, #ccc)",
  borderRadius: 6,
  background: "transparent",
  cursor: "pointer",
  fontSize: 14,
};
