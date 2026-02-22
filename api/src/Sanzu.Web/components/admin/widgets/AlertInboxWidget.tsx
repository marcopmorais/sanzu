import type { AlertCounts } from "@/lib/api-client/generated/admin";

interface AlertInboxWidgetProps {
  alerts: AlertCounts;
}

export function AlertInboxWidget({ alerts }: AlertInboxWidgetProps) {
  const total = alerts.critical + alerts.warning + alerts.info;

  return (
    <section className="panel" data-testid="alert-inbox-widget">
      <h2>Alert Inbox</h2>
      <p className="meta">Active alerts by severity.</p>
      <p style={{ fontSize: 32, fontWeight: 700 }}>{total}</p>
      <div className="kpi-grid" style={{ marginTop: 8 }}>
        <div className="kpi-card">
          <span className="meta">Critical</span>
          <strong style={{ color: alerts.critical > 0 ? "#c00" : "inherit" }}>
            {alerts.critical}
          </strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Warning</span>
          <strong style={{ color: alerts.warning > 0 ? "var(--warn)" : "inherit" }}>
            {alerts.warning}
          </strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Info</span>
          <strong>{alerts.info}</strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Unacked</span>
          <strong style={{ color: alerts.unacknowledged > 0 ? "var(--warn)" : "inherit" }}>
            {alerts.unacknowledged}
          </strong>
        </div>
      </div>
    </section>
  );
}
