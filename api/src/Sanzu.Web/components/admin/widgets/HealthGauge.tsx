import type { HealthBand } from "@/lib/api-client/generated/admin";

interface HealthGaugeProps {
  score: number | null;
  band: HealthBand | null;
  primaryIssue?: string | null;
}

const BAND_COLORS: Record<string, string> = {
  Green: "#1e8f4d",
  Yellow: "#b85a2a",
  Red: "#cc0000",
};

export function HealthGauge({ score, band, primaryIssue }: HealthGaugeProps) {
  if (score === null || band === null) {
    return (
      <div className="health-gauge" data-testid="health-gauge">
        <p className="meta">No health score available</p>
      </div>
    );
  }

  const color = BAND_COLORS[band] ?? "#666";
  const label = `Health score: ${score} out of 100, ${band}`;

  return (
    <div className="health-gauge" data-testid="health-gauge">
      <div
        role="meter"
        aria-valuenow={score}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={label}
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          width: 80,
          height: 80,
          borderRadius: "50%",
          border: `4px solid ${color}`,
          fontSize: 24,
          fontWeight: 700,
          color,
        }}
      >
        {score}
      </div>
      <p style={{ color, fontWeight: 600, marginTop: 4 }}>{band}</p>
      {primaryIssue && (band === "Yellow" || band === "Red") && (
        <p className="meta" data-testid="health-gauge-issue">
          {primaryIssue}
        </p>
      )}
    </div>
  );
}
