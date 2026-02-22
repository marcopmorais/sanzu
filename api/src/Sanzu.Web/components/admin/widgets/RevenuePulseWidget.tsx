import type { RevenuePulse } from "@/lib/api-client/generated/admin";

interface RevenuePulseWidgetProps {
  revenue: RevenuePulse;
}

function formatEur(value: number): string {
  return new Intl.NumberFormat("en", {
    style: "currency",
    currency: "EUR",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
}

export function RevenuePulseWidget({ revenue }: RevenuePulseWidgetProps) {
  return (
    <section className="panel" data-testid="revenue-pulse-widget">
      <h2>Revenue Pulse</h2>
      <p className="meta">Monthly and annual recurring revenue.</p>
      <div className="kpi-grid" style={{ marginTop: 8 }}>
        <div className="kpi-card">
          <span className="meta">MRR</span>
          <strong>{formatEur(revenue.mrr)}</strong>
        </div>
        <div className="kpi-card">
          <span className="meta">ARR</span>
          <strong>{formatEur(revenue.arr)}</strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Churn</span>
          <strong style={{ color: revenue.churnRate > 5 ? "var(--warn)" : "inherit" }}>
            {revenue.churnRate}%
          </strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Growth</span>
          <strong style={{ color: "var(--ok)" }}>{revenue.growthRate}%</strong>
        </div>
      </div>
    </section>
  );
}
