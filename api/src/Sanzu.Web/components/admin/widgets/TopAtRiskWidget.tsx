import type { AtRiskTenant } from "@/lib/api-client/generated/admin";

interface TopAtRiskWidgetProps {
  topAtRisk: AtRiskTenant[];
}

export function TopAtRiskWidget({ topAtRisk }: TopAtRiskWidgetProps) {
  return (
    <section className="panel" data-testid="top-at-risk-widget">
      <h2>Top At-Risk Tenants</h2>
      <p className="meta">Lowest health scores requiring attention.</p>
      {topAtRisk.length === 0 ? (
        <p className="meta" style={{ marginTop: 8 }}>No at-risk tenants</p>
      ) : (
        <table className="table" style={{ marginTop: 8 }} aria-label="At-risk tenants">
          <thead>
            <tr>
              <th>Tenant</th>
              <th>Score</th>
              <th>Issue</th>
            </tr>
          </thead>
          <tbody>
            {topAtRisk.map((tenant) => (
              <tr key={tenant.tenantId}>
                <td>
                  <a href={`/app/admin/tenants/${tenant.tenantId}`}>
                    {tenant.name}
                  </a>
                </td>
                <td>
                  <strong style={{ color: "#c00" }}>{tenant.score}</strong>
                </td>
                <td>{tenant.primaryIssue || "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
