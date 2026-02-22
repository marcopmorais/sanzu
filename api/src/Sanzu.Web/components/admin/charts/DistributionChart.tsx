"use client";

import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell } from "recharts";

interface ChartDataItem {
  name: string;
  value: number;
  fill: string;
}

interface DistributionChartProps {
  data: ChartDataItem[];
  ariaLabel: string;
}

export function DistributionChart({ data, ariaLabel }: DistributionChartProps) {
  return (
    <div role="img" aria-label={ariaLabel} data-testid="distribution-chart">
      <ResponsiveContainer width="100%" height={180}>
        <BarChart data={data} margin={{ top: 8, right: 8, bottom: 8, left: 0 }}>
          <XAxis dataKey="name" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
          <Tooltip />
          <Bar dataKey="value" radius={[4, 4, 0, 0]}>
            {data.map((entry, index) => (
              <Cell key={index} fill={entry.fill} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
