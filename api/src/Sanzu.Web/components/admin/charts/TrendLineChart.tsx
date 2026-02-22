"use client";

import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

export interface TrendLineChartProps {
  data: { periodLabel: string; mrr: number; tenantCount: number }[];
}

export function TrendLineChart({ data }: TrendLineChartProps) {
  if (data.length === 0) {
    return <p className="meta">No trend data available.</p>;
  }

  return (
    <div role="img" aria-label={`MRR trend chart with ${data.length} data points`}>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={data} margin={{ top: 5, right: 20, bottom: 5, left: 20 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="periodLabel" fontSize={12} />
          <YAxis fontSize={12} />
          <Tooltip formatter={(value: number) => [`€${value.toLocaleString()}`, "MRR"]} />
          <Line
            type="monotone"
            dataKey="mrr"
            stroke="var(--primary, #2563eb)"
            strokeWidth={2}
            dot={false}
          />
        </LineChart>
      </ResponsiveContainer>
      <p className="sr-only">
        MRR trend from {data[0].periodLabel} to {data[data.length - 1].periodLabel}.
        Latest MRR: €{data[data.length - 1].mrr.toLocaleString()}.
      </p>
    </div>
  );
}
