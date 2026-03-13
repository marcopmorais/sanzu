"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useAdminPermissions } from "@/lib/admin/AdminPermissionsContext";
import {
  getTenantSummary,
  getTenantBilling,
  getTenantCases,
  getTenantActivity,
  getTenantComms,
  overrideBlockedStep,
  extendGracePeriod,
  triggerReOnboarding,
  startImpersonation,
  sendCommunication,
  type TenantSummary,
  type TenantBilling,
  type TenantCases,
  type TenantActivity,
  type CommItem,
} from "@/lib/api-client/generated/admin";
import { HealthGauge } from "@/components/admin/widgets/HealthGauge";

type TabKey = "summary" | "billing" | "cases" | "activity" | "actions" | "comms";

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
  { key: "actions", label: "Actions", endpointPattern: "/admin/tenants/*/actions" },
  { key: "comms", label: "Comms", endpointPattern: "/admin/tenants/*/comms" },
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
  const [comms, setComms] = useState<CommItem[] | null>(null);

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
        } else if (activeTab === "comms" && !comms) {
          const data = await getTenantComms(tenantId);
          if (!cancelled) setComms(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : `Failed to load ${activeTab} data`);
        }
      }
    }

    loadTabData();
    return () => { cancelled = true; };
  }, [activeTab, tenantId, billing, cases, activity, comms]);

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
        {activeTab === "actions" && <ActionsTabContent tenantId={tenantId} />}
        {activeTab === "comms" && <CommsTabContent tenantId={tenantId} comms={comms} onRefresh={() => getTenantComms(tenantId).then(setComms)} />}
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

function ActionsTabContent({ tenantId }: { tenantId: string }) {
  const [actionMsg, setActionMsg] = useState<string | null>(null);

  const [graceRationale, setGraceRationale] = useState("");
  const [graceDays, setGraceDays] = useState(7);
  const [overrideCaseId, setOverrideCaseId] = useState("");
  const [overrideStepId, setOverrideStepId] = useState("");
  const [overrideRationale, setOverrideRationale] = useState("");

  async function handleExtendGrace() {
    if (!graceRationale.trim()) { setActionMsg("Rationale is required"); return; }
    try {
      await extendGracePeriod(tenantId, graceDays, graceRationale);
      setActionMsg("Grace period extended successfully");
      setGraceRationale("");
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : "Failed");
    }
  }

  async function handleOverrideStep() {
    if (!overrideRationale.trim()) { setActionMsg("Rationale is required"); return; }
    try {
      await overrideBlockedStep(tenantId, overrideCaseId, overrideStepId, overrideRationale);
      setActionMsg("Blocked step overridden successfully");
      setOverrideRationale("");
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : "Failed");
    }
  }

  async function handleReOnboard() {
    if (!confirm("Are you sure you want to trigger re-onboarding for this tenant?")) return;
    try {
      await triggerReOnboarding(tenantId);
      setActionMsg("Re-onboarding triggered successfully");
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : "Failed");
    }
  }

  async function handleImpersonate() {
    try {
      const result = await startImpersonation(tenantId);
      setActionMsg(`Impersonation started — expires at ${new Date(result.expiresAt).toLocaleTimeString()}`);
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : "Failed");
    }
  }

  const actionBtnStyle: React.CSSProperties = {
    padding: "6px 14px", fontSize: 13, border: "1px solid var(--border, #ccc)",
    borderRadius: 4, background: "transparent", cursor: "pointer", marginTop: 8,
  };

  const inputStyle: React.CSSProperties = {
    padding: "4px 8px", fontSize: 13, border: "1px solid var(--border, #ccc)",
    borderRadius: 4, width: "100%",
  };

  return (
    <section className="panel" data-testid="tab-content-actions">
      <h2>Support Actions</h2>
      {actionMsg && <p className="meta" style={{ marginBottom: 10 }}>{actionMsg}</p>}

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20, marginTop: 10 }}>
        <div style={{ border: "1px solid #e0e0e0", borderRadius: 6, padding: 14 }}>
          <h3>Override Blocked Step</h3>
          <label className="meta">Case ID<br /><input style={inputStyle} value={overrideCaseId} onChange={(e) => setOverrideCaseId(e.target.value)} /></label>
          <label className="meta" style={{ marginTop: 6, display: "block" }}>Step ID<br /><input style={inputStyle} value={overrideStepId} onChange={(e) => setOverrideStepId(e.target.value)} /></label>
          <label className="meta" style={{ marginTop: 6, display: "block" }}>Rationale (required)<br /><textarea style={{ ...inputStyle, minHeight: 60 }} value={overrideRationale} onChange={(e) => setOverrideRationale(e.target.value)} /></label>
          <button style={actionBtnStyle} onClick={handleOverrideStep}>Override Step</button>
        </div>

        <div style={{ border: "1px solid #e0e0e0", borderRadius: 6, padding: 14 }}>
          <h3>Extend Grace Period</h3>
          <label className="meta">Days<br /><input type="number" style={inputStyle} value={graceDays} onChange={(e) => setGraceDays(Number(e.target.value))} min={1} max={90} /></label>
          <label className="meta" style={{ marginTop: 6, display: "block" }}>Rationale (required)<br /><textarea style={{ ...inputStyle, minHeight: 60 }} value={graceRationale} onChange={(e) => setGraceRationale(e.target.value)} /></label>
          <button style={actionBtnStyle} onClick={handleExtendGrace}>Extend Grace Period</button>
        </div>

        <div style={{ border: "1px solid #e0e0e0", borderRadius: 6, padding: 14 }}>
          <h3>Re-Onboarding</h3>
          <p className="meta">Reset the tenant onboarding flow to allow re-onboarding.</p>
          <button style={actionBtnStyle} onClick={handleReOnboard}>Trigger Re-Onboarding</button>
        </div>

        <div style={{ border: "1px solid #e0e0e0", borderRadius: 6, padding: 14 }}>
          <h3>Impersonate Tenant</h3>
          <p className="meta">Start a read-only impersonation session (30 min).</p>
          <button style={actionBtnStyle} onClick={handleImpersonate}>Start Impersonation</button>
        </div>
      </div>
    </section>
  );
}

function CommsTabContent({ tenantId, comms, onRefresh }: { tenantId: string; comms: CommItem[] | null; onRefresh: () => void }) {
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [messageType, setMessageType] = useState("support");
  const [sending, setSending] = useState(false);

  async function handleSend() {
    if (!subject.trim() || !body.trim()) return;
    setSending(true);
    try {
      await sendCommunication(tenantId, subject, body, messageType);
      setSubject("");
      setBody("");
      onRefresh();
    } catch {
      // Silent
    } finally {
      setSending(false);
    }
  }

  const inputStyle: React.CSSProperties = {
    padding: "4px 8px", fontSize: 13, border: "1px solid var(--border, #ccc)", borderRadius: 4, width: "100%",
  };

  return (
    <section className="panel" data-testid="tab-content-comms">
      <h2>Communications</h2>

      <div style={{ border: "1px solid #e0e0e0", borderRadius: 6, padding: 14, marginBottom: 14 }}>
        <h3>Send Communication</h3>
        <div style={{ display: "flex", gap: 10, marginBottom: 8 }}>
          <label className="meta">Type{" "}
            <select value={messageType} onChange={(e) => setMessageType(e.target.value)} style={inputStyle}>
              <option value="support">Support</option>
              <option value="billing">Billing</option>
              <option value="onboarding">Onboarding</option>
              <option value="escalation">Escalation</option>
            </select>
          </label>
        </div>
        <label className="meta">Subject<br /><input style={inputStyle} value={subject} onChange={(e) => setSubject(e.target.value)} /></label>
        <label className="meta" style={{ marginTop: 6, display: "block" }}>Message<br /><textarea style={{ ...inputStyle, minHeight: 80 }} value={body} onChange={(e) => setBody(e.target.value)} /></label>
        <button style={{ padding: "6px 14px", fontSize: 13, border: "1px solid var(--border, #ccc)", borderRadius: 4, background: "transparent", cursor: "pointer", marginTop: 8 }} onClick={handleSend} disabled={sending}>
          {sending ? "Sending..." : "Send"}
        </button>
      </div>

      <h3>Communication History</h3>
      {!comms ? (
        <p className="meta">Loading communications...</p>
      ) : comms.length === 0 ? (
        <p className="meta">No communications found.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
          <thead>
            <tr>
              <th style={{ textAlign: "left", padding: "8px 12px", borderBottom: "2px solid #e0e0e0", fontWeight: 600 }}>Date</th>
              <th style={{ textAlign: "left", padding: "8px 12px", borderBottom: "2px solid #e0e0e0", fontWeight: 600 }}>Sender</th>
              <th style={{ textAlign: "left", padding: "8px 12px", borderBottom: "2px solid #e0e0e0", fontWeight: 600 }}>Type</th>
              <th style={{ textAlign: "left", padding: "8px 12px", borderBottom: "2px solid #e0e0e0", fontWeight: 600 }}>Subject</th>
            </tr>
          </thead>
          <tbody>
            {comms.map((c) => (
              <tr key={c.id}>
                <td style={{ padding: "8px 12px", borderBottom: "1px solid #e0e0e0" }}>{new Date(c.createdAt).toLocaleString()}</td>
                <td style={{ padding: "8px 12px", borderBottom: "1px solid #e0e0e0" }}>{c.senderName}</td>
                <td style={{ padding: "8px 12px", borderBottom: "1px solid #e0e0e0" }}>{c.messageType}</td>
                <td style={{ padding: "8px 12px", borderBottom: "1px solid #e0e0e0" }}>{c.subject}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
