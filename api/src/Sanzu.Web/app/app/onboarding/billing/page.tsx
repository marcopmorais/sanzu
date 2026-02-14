import Link from "next/link";
import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function OnboardingBillingPage() {
  return (
    <main>
      <h1>Billing Activation</h1>
      <p className="meta">Story 1.7 route: plan selection, billing activation, and recovery hints.</p>
      <div className="panel grid">
        <div className="grid two">
          <section className="panel">
            <h2>Plan Selection</h2>
            <p className="meta">Growth and Scale tiers with proration preview support.</p>
            <div style={{ display: "flex", gap: 8 }}>
              <Button label="Preview Plan Change" variant="secondary" />
              <Button label="Activate Subscription" />
            </div>
          </section>
          <section className="panel">
            <h2>Payment State</h2>
            <StatusBanner kind="warn" text="Payment requires confirmation. Retry available." />
          </section>
        </div>
        <div style={{ display: "flex", gap: 12 }}>
          <Link href="/app/settings/billing/history">Billing History</Link>
          <Link href="/app/settings/billing/recovery">Payment Recovery</Link>
          <Link href="/app/onboarding/complete">Finish Onboarding</Link>
        </div>
      </div>
    </main>
  );
}
