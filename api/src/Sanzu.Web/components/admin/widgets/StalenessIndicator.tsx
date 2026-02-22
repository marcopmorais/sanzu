"use client";

interface StalenessIndicatorProps {
  computedAt: string;
  isStale: boolean;
  intervalMinutes?: number;
}

export function StalenessIndicator({
  computedAt,
  isStale,
  intervalMinutes = 5,
}: StalenessIndicatorProps) {
  const computed = new Date(computedAt);
  const ageMs = Date.now() - computed.getTime();
  const ageMinutes = Math.floor(ageMs / 60_000);
  const redThreshold = intervalMinutes * 5;
  const isRed = ageMinutes > redThreshold;

  let timeAgo: string;
  if (ageMinutes < 1) {
    timeAgo = "just now";
  } else if (ageMinutes < 60) {
    timeAgo = `${ageMinutes} min ago`;
  } else {
    const hours = Math.floor(ageMinutes / 60);
    timeAgo = `${hours}h ${ageMinutes % 60}m ago`;
  }

  let style: React.CSSProperties;
  let label: string;

  if (!isStale) {
    style = {
      color: "var(--muted)",
      fontSize: 12,
    };
    label = `Last updated: ${timeAgo}`;
  } else if (isRed) {
    style = {
      color: "#c00",
      fontWeight: 600,
      fontSize: 12,
    };
    label = `⚠ Data is stale — last updated ${timeAgo}`;
  } else {
    style = {
      color: "var(--warn)",
      fontWeight: 600,
      fontSize: 12,
    };
    label = `Data may be stale — last updated ${timeAgo}`;
  }

  return (
    <p aria-live="polite" style={style} data-testid="staleness-indicator">
      {label}
    </p>
  );
}
