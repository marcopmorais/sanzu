"use client";

import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";

export interface PlanDistributionChartProps {
  data: { planName: string; tenantCount: number; mrr: number; percentage: number }[];
}

const COLORS = ["#2563eb", "#7c3aed", "#059669", "#d97706", "#dc2626"];

export function PlanDistributionChart({ data }: PlanDistributionChartProps) {
  if (data.length === 0) {
    return <p className="meta">No plan distribution data available.</p>;
  }

  return (
    <div role="img" aria-label={`Revenue distribution across ${data.length} plans`} data-testid="plan-distribution-chart">
      <ResponsiveContainer width="100%" height={300}>
        <PieChart>
          <Pie
            data={data}
            dataKey="mrr"
            nameKey="planName"
            cx="50%"
            cy="50%"
            outerRadius={100}
            label={({ planName, percentage }) => `${planName} (${percentage}%)`}
          >
            {data.map((_, index) => (
              <Cell key={index} fill={COLORS[index % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip formatter={(value: number) => [`€${value.toLocaleString()}`, "MRR"]} />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
      <p className="sr-only">
        Revenue breakdown by plan:{" "}
        {data.map((d) => `${d.planName}: €${d.mrr.toLocaleString()} (${d.percentage}%)`).join(", ")}.
      </p>
    </div>
  );
}
