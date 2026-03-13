"use client";

import { useEffect, useState } from "react";
import {
  getAlerts,
  updateAlertStatus,
  type AdminAlertItem,
} from "@/lib/api-client/generated/admin";

const SEVERITY_COLORS: Record<string, string> = {
  Critical: "#dc2626",
  Warning: "#d97706",
  Info: "#2563eb",
};

const STATUS_OPTIONS = ["", "Fired", "Acknowledged", "Resolved", "Dismissed"];
const SEVERITY_OPTIONS = ["", "Critical", "Warning", "Info"];

export default function AlertInboxPage() {
  const [alerts, setAlerts] = useState<AdminAlertItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("");
  const [severityFilter, setSeverityFilter] = useState("");

  async function loadAlerts() {
    try {
      const data = await getAlerts({
        status: statusFilter || undefined,
        severity: severityFilter || undefined,
      });
      setAlerts(data);
      setLoading(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load alerts");
      setLoading(false);
    }
  }

  useEffect(() => {
    loadAlerts();
  }, [statusFilter, severityFilter]);

  async function handleAction(alertId: string, action: "Acknowledged" | "Resolved") {
    try {
      await updateAlertStatus(alertId, action);
      await loadAlerts();
    } catch {
      // Silently handle — user sees stale state
    }
  }

  if (loading) {
    return (
      <main>
        <p className="meta">Loading alerts...</p>
      </main>
    );
  }

  if (error) {
    return (
      <main>
        <h1>Alert Inbox</h1>
        <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>
      </main>
    );
  }

  return (
    <main>
      <h1>Alert Inbox</h1>
      <p className="meta">Operational alerts requiring attention.</p>

      <div style={{ display: "flex", gap: 12, marginTop: 14, marginBottom: 14 }}>
        <label>
          Status:{" "}
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>{s || "All"}</option>
            ))}
          </select>
        </label>
        <label>
          Severity:{" "}
          <select value={severityFilter} onChange={(e) => setSeverityFilter(e.target.value)}>
            {SEVERITY_OPTIONS.map((s) => (
              <option key={s} value={s}>{s || "All"}</option>
            ))}
          </select>
        </label>
      </div>

      {alerts.length === 0 ? (
        <p className="meta">No alerts match the current filters.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
          <thead>
            <tr>
              <th style={thStyle}>Severity</th>
              <th style={thStyle}>Title</th>
              <th style={thStyle}>Tenant</th>
              <th style={thStyle}>Routed To</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>Fired</th>
              <th style={thStyle}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {alerts.map((alert) => (
              <tr key={alert.id}>
                <td style={tdStyle}>
                  <span
                    style={{
                      display: "inline-block",
                      width: 10,
                      height: 10,
                      borderRadius: "50%",
                      backgroundColor: SEVERITY_COLORS[alert.severity] ?? "#999",
                      marginRight: 6,
                    }}
                    aria-label={alert.severity}
                  />
                  {alert.severity}
                </td>
                <td style={tdStyle}>{alert.title}</td>
                <td style={tdStyle}>
                  {alert.tenantId ? (
                    <a href={`/app/admin/tenants/${alert.tenantId}`}>{alert.tenantName ?? "Unknown"}</a>
                  ) : (
                    "—"
                  )}
                </td>
                <td style={tdStyle}>{alert.routedToRole}</td>
                <td style={tdStyle}>{alert.status}</td>
                <td style={tdStyle}>{new Date(alert.firedAt).toLocaleString()}</td>
                <td style={tdStyle}>
                  {alert.status === "Fired" && (
                    <button
                      onClick={() => handleAction(alert.id, "Acknowledged")}
                      style={actionBtnStyle}
                    >
                      Acknowledge
                    </button>
                  )}
                  {(alert.status === "Fired" || alert.status === "Acknowledged") && (
                    <button
                      onClick={() => handleAction(alert.id, "Resolved")}
                      style={{ ...actionBtnStyle, marginLeft: 4 }}
                    >
                      Resolve
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
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

const actionBtnStyle: React.CSSProperties = {
  padding: "4px 10px",
  fontSize: 12,
  border: "1px solid var(--border, #ccc)",
  borderRadius: 4,
  background: "transparent",
  cursor: "pointer",
};
