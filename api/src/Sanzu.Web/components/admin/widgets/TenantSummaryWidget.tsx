import type { TenantCounts } from "@/lib/api-client/generated/admin";

interface TenantSummaryWidgetProps {
  tenants: TenantCounts;
}

export function TenantSummaryWidget({ tenants }: TenantSummaryWidgetProps) {
  return (
    <section className="panel" data-testid="tenant-summary-widget">
      <h2>Tenant Summary</h2>
      <p className="meta">Portfolio overview by lifecycle status.</p>
      <p style={{ fontSize: 32, fontWeight: 700 }}>{tenants.total}</p>
      <div className="kpi-grid" style={{ marginTop: 8 }}>
        <div className="kpi-card">
          <span className="meta">Active</span>
          <strong>{tenants.active}</strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Trial</span>
          <strong>{tenants.trial}</strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Churning</span>
          <strong style={{ color: tenants.churning > 0 ? "var(--warn)" : "inherit" }}>
            {tenants.churning}
          </strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Suspended</span>
          <strong style={{ color: tenants.suspended > 0 ? "#c00" : "inherit" }}>
            {tenants.suspended}
          </strong>
        </div>
      </div>
    </section>
  );
}
