"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useAdminPermissions } from "@/lib/admin/AdminPermissionsContext";
import {
  getTenantSummary,
  getTenantBilling,
  getTenantCases,
  getTenantActivity,
  type TenantSummary,
  type TenantBilling,
  type TenantCases,
  type TenantActivity,
} from "@/lib/api-client/generated/admin";
import { HealthGauge } from "@/components/admin/widgets/HealthGauge";

type TabKey = "summary" | "billing" | "cases" | "activity";

interface TabDef {
  key: TabKey;
  label: string;
  endpointPattern: string | null; // null = always visible
}

const TABS: TabDef[] = [
  { key: "summary", label: "Summary", endpointPattern: null },
  { key: "billing", label: "Billing", endpointPattern: "/admin/tenants/*/billing" },
  { key: "cases", label: "Cases", endpointPattern: "/admin/tenants/*/cases" },
  { key: "activity", label: "Activity", endpointPattern: "/admin/tenants/*/activity" },
];

const BILLING_HEALTH_COLORS: Record<string, string> = {
  Paid: "#1e8f4d",
  Overdue: "#b85a2a",
  Failed: "#cc0000",
};

export default function Tenant360Page() {
  const params = useParams();
  const tenantId = params.tenantId as string;
  const { permissions } = useAdminPermissions();

  const [activeTab, setActiveTab] = useState<TabKey>("summary");

  // Tab data
  const [summary, setSummary] = useState<TenantSummary | null>(null);
  const [billing, setBilling] = useState<TenantBilling | null>(null);
  const [cases, setCases] = useState<TenantCases | null>(null);
  const [activity, setActivity] = useState<TenantActivity | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Determine visible tabs based on permissions
  const visibleTabs = TABS.filter((tab) => {
    if (!tab.endpointPattern) return true;
    if (!permissions) return false;
    return permissions.accessibleEndpoints.some(
      (ep) => ep === tab.endpointPattern || tab.endpointPattern!.startsWith(ep.replace("/*", ""))
    );
  });

  // Load summary on mount
  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const data = await getTenantSummary(tenantId);
        if (!cancelled) { setSummary(data); setLoading(false); }
      } catch (err) {
        if (!cancelled) { setError(err instanceof Error ? err.message : "Failed to load tenant"); setLoading(false); }
      }
    }
    load();
    return () => { cancelled = true; };
  }, [tenantId]);

  // Load tab data on tab change
  useEffect(() => {
    let cancelled = false;

    async function loadTabData() {
      try {
        if (activeTab === "billing" && !billing) {
          const data = await getTenantBilling(tenantId);
          if (!cancelled) setBilling(data);
        } else if (activeTab === "cases" && !cases) {
          const data = await getTenantCases(tenantId);
          if (!cancelled) setCases(data);
        } else if (activeTab === "activity" && !activity) {
          const data = await getTenantActivity(tenantId);
          if (!cancelled) setActivity(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : `Failed to load ${activeTab} data`);
        }
      }
    }

    loadTabData();
    return () => { cancelled = true; };
  }, [activeTab, tenantId, billing, cases, activity]);

  if (loading) {
    return (
      <main>
        <p className="meta">Loading tenant details...</p>
      </main>
    );
  }

  if (error && !summary) {
    return (
      <main>
        <h1>Tenant 360</h1>
        <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>
      </main>
    );
  }

  return (
    <main>
      <h1>{summary?.name ?? "Tenant 360"}</h1>
      <p className="meta">
        {summary?.status} &middot; {summary?.planTier ?? "No plan"} &middot; {summary?.region ?? "No region"}
      </p>

      <nav style={{ display: "flex", gap: 0, marginTop: 14, borderBottom: "2px solid #e0e0e0" }}>
        {visibleTabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            style={{
              padding: "8px 16px",
              border: "none",
              borderBottom: activeTab === tab.key ? "2px solid #333" : "2px solid transparent",
              background: "none",
              fontWeight: activeTab === tab.key ? 700 : 400,
              cursor: "pointer",
              marginBottom: -2,
            }}
            aria-current={activeTab === tab.key ? "page" : undefined}
            data-testid={`tab-${tab.key}`}
          >
            {tab.label}
          </button>
        ))}
      </nav>

      <div style={{ marginTop: 14 }}>
        {error && (
          <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>
        )}

        {activeTab === "summary" && summary && <SummaryTabContent summary={summary} />}
        {activeTab === "billing" && <BillingTabContent billing={billing} />}
        {activeTab === "cases" && <CasesTabContent cases={cases} />}
        {activeTab === "activity" && <ActivityTabContent activity={activity} />}
      </div>
    </main>
  );
}

// ── Tab Content Components ──

function SummaryTabContent({ summary }: { summary: TenantSummary }) {
  return (
    <section className="panel" data-testid="tab-content-summary">
      <div style={{ display: "flex", gap: 24, alignItems: "flex-start" }}>
        <div style={{ flex: 1 }}>
          <h2>Tenant Details</h2>
          <dl style={{ display: "grid", gridTemplateColumns: "auto 1fr", gap: "6px 14px" }}>
            <dt className="meta">Name</dt><dd>{summary.name}</dd>
            <dt className="meta">Status</dt><dd>{summary.status}</dd>
            <dt className="meta">Plan</dt><dd>{summary.planTier ?? "—"}</dd>
            <dt className="meta">Region</dt><dd>{summary.region ?? "—"}</dd>
            <dt className="meta">Contact</dt><dd>{summary.contactEmail ?? "—"}</dd>
            <dt className="meta">Signup Date</dt><dd>{new Date(summary.signupDate).toLocaleDateString()}</dd>
          </dl>
        </div>
        <div style={{ textAlign: "center" }}>
          <h2>Health Score</h2>
          <HealthGauge score={summary.healthScore} band={summary.healthBand} />
        </div>
      </div>
    </section>
  );
}

function BillingTabContent({ billing }: { billing: TenantBilling | null }) {
  if (!billing) {
    return <p className="meta">Loading billing data...</p>;
  }

  const healthColor = BILLING_HEALTH_COLORS[billing.billingHealth] ?? "#666";

  return (
    <section className="panel" data-testid="tab-content-billing">
      <h2>Billing</h2>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 14, marginBottom: 14 }}>
        <div className="kpi-card">
          <p className="meta">Plan</p>
          <p style={{ fontWeight: 600 }}>{billing.subscriptionPlan ?? "—"}</p>
        </div>
        <div className="kpi-card">
          <p className="meta">Billing Health</p>
          <p style={{ fontWeight: 600, color: healthColor }}>{billing.billingHealth}</p>
        </div>
        <div className="kpi-card">
          <p className="meta">Billing Cycle</p>
          <p style={{ fontWeight: 600 }}>{billing.billingCycle ?? "—"}</p>
        </div>
      </div>

      {billing.gracePeriodActive && (
        <p className="meta" style={{ color: "#b85a2a", marginBottom: 10 }}>
          Grace period active — retry at {billing.gracePeriodRetryAt
            ? new Date(billing.gracePeriodRetryAt).toLocaleString()
            : "N/A"}
        </p>
      )}

      <h3>Recent Invoices</h3>
      <table className="table" data-testid="billing-invoices-table">
        <thead>
          <tr>
            <th>Invoice</th>
            <th>Period</th>
            <th>Amount</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {billing.recentInvoices.map((inv) => (
            <tr key={inv.invoiceNumber}>
              <td>{inv.invoiceNumber}</td>
              <td>{new Date(inv.billingCycleStart).toLocaleDateString()} – {new Date(inv.billingCycleEnd).toLocaleDateString()}</td>
              <td>{inv.currency} {inv.totalAmount.toFixed(2)}</td>
              <td>{inv.status}</td>
            </tr>
          ))}
          {billing.recentInvoices.length === 0 && (
            <tr><td colSpan={4} style={{ textAlign: "center" }}><p className="meta">No invoices</p></td></tr>
          )}
        </tbody>
      </table>
    </section>
  );
}

function CasesTabContent({ cases }: { cases: TenantCases | null }) {
  if (!cases) {
    return <p className="meta">Loading cases data...</p>;
  }

  return (
    <section className="panel" data-testid="tab-content-cases">
      <h2>Cases</h2>
      {cases.cases.length === 0 ? (
        <p className="meta">No cases found for this tenant.</p>
      ) : (
        <table className="table" data-testid="cases-table">
          <thead>
            <tr>
              <th>Case</th>
              <th>Deceased</th>
              <th>Status</th>
              <th>Workflow</th>
              <th>Progress</th>
              <th>Blocked</th>
            </tr>
          </thead>
          <tbody>
            {cases.cases.map((c) => (
              <tr key={c.caseId}>
                <td>{c.caseNumber}</td>
                <td>{c.deceasedFullName}</td>
                <td>{c.status}</td>
                <td>{c.workflowKey ?? "—"}</td>
                <td>
                  {c.workflowProgress.completedSteps}/{c.workflowProgress.totalSteps} completed
                  {c.workflowProgress.inProgressSteps > 0 && `, ${c.workflowProgress.inProgressSteps} in progress`}
                </td>
                <td>
                  {c.workflowProgress.blockedSteps > 0 ? (
                    <span style={{ color: "#cc0000", fontWeight: 600 }}>
                      {c.workflowProgress.blockedSteps} blocked
                    </span>
                  ) : (
                    <span className="meta">0</span>
                  )}
                  {c.blockedSteps.length > 0 && (
                    <ul style={{ margin: "4px 0 0", paddingLeft: 16, fontSize: "0.9em" }}>
                      {c.blockedSteps.map((bs) => (
                        <li key={bs.stepKey}>
                          <strong>{bs.title}</strong>
                          {bs.blockedReasonCode && ` — ${bs.blockedReasonCode}`}
                          {bs.blockedReasonDetail && (
                            <span className="meta"> ({bs.blockedReasonDetail})</span>
                          )}
                        </li>
                      ))}
                    </ul>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function ActivityTabContent({ activity }: { activity: TenantActivity | null }) {
  if (!activity) {
    return <p className="meta">Loading activity data...</p>;
  }

  return (
    <section className="panel" data-testid="tab-content-activity">
      <h2>Activity Timeline</h2>
      {activity.events.length === 0 ? (
        <p className="meta">No recent activity (last 30 days).</p>
      ) : (
        <table className="table" data-testid="activity-table">
          <thead>
            <tr>
              <th>Event</th>
              <th>Actor</th>
              <th>Timestamp</th>
              <th>Case</th>
            </tr>
          </thead>
          <tbody>
            {activity.events.map((event, i) => (
              <tr key={`${event.timestamp}-${i}`}>
                <td style={{ fontWeight: 600 }}>{event.eventType}</td>
                <td className="meta">{event.actorUserId.substring(0, 8)}...</td>
                <td>{new Date(event.timestamp).toLocaleString()}</td>
                <td>{event.caseId ? event.caseId.substring(0, 8) + "..." : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
