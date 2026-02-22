import type { HealthDistribution } from "@/lib/api-client/generated/admin";
import { DistributionChart } from "@/components/admin/charts/DistributionChart";

interface HealthOverviewWidgetProps {
  health: HealthDistribution;
}

export function HealthOverviewWidget({ health }: HealthOverviewWidgetProps) {
  const chartData = [
    { name: "Green", value: health.green, fill: "#1e8f4d" },
    { name: "Yellow", value: health.yellow, fill: "#b85a2a" },
    { name: "Red", value: health.red, fill: "#cc0000" },
  ];

  const total = health.green + health.yellow + health.red;
  const ariaLabel = `Health distribution: ${health.green} green, ${health.yellow} yellow, ${health.red} red out of ${total} tenants`;

  return (
    <section className="panel" data-testid="health-overview-widget">
      <h2>Health Overview</h2>
      <p className="meta">Tenant health band distribution.</p>
      <DistributionChart data={chartData} ariaLabel={ariaLabel} />
    </section>
  );
}
