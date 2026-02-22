import type { OnboardingStatus } from "@/lib/api-client/generated/admin";

interface OnboardingStatusWidgetProps {
  onboarding: OnboardingStatus;
}

export function OnboardingStatusWidget({ onboarding }: OnboardingStatusWidgetProps) {
  return (
    <section className="panel" data-testid="onboarding-status-widget">
      <h2>Onboarding Status</h2>
      <p className="meta">Signup-to-active conversion health.</p>
      <div className="kpi-grid" style={{ marginTop: 8 }}>
        <div className="kpi-card">
          <span className="meta">Completion Rate</span>
          <strong style={{ color: onboarding.completionRate >= 80 ? "var(--ok)" : "var(--warn)" }}>
            {onboarding.completionRate}%
          </strong>
        </div>
        <div className="kpi-card">
          <span className="meta">Stalled</span>
          <strong style={{ color: onboarding.stalled > 0 ? "var(--warn)" : "inherit" }}>
            {onboarding.stalled}
          </strong>
        </div>
      </div>
    </section>
  );
}
