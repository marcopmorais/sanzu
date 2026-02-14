import Link from "next/link";
import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function OnboardingBillingPage() {
  return (
    <main>
      <h1>Billing Activation</h1>
      <p className="meta">Story 1.7 route: plan selection, billing activation, and recovery hints.</p>
      <div className="hero">
        <h2>Activation checklist</h2>
        <p className="meta">Select plan, confirm billing contact, then submit payment method for activation.</p>
      </div>
      <div className="panel grid">
        <div className="grid two">
          <section className="panel">
            <h2>Plan Selection</h2>
            <p className="meta">Growth and Scale tiers with proration preview support and confirmation checkpoint.</p>
            <ul aria-label="Available plans">
              <li>Growth - $299/month - includes onboarding + billing baseline</li>
              <li>Scale - $499/month - includes advanced governance + KPI controls</li>
            </ul>
            <div className="actions">
              <Button label="Preview Plan Change" variant="secondary" />
              <Button label="Activate Subscription" />
            </div>
            <p className="meta">Confirmation required: "I approve recurring monthly billing for this tenant."</p>
          </section>
          <section className="panel">
            <h2>Payment State</h2>
            <StatusBanner kind="warn" text="Payment requires confirmation. Retry available." />
            <p className="meta" style={{ marginTop: 8 }}>
              Last retry: 2026-02-14 12:30 UTC | Next retry: 2026-02-15 12:30 UTC
            </p>
            <ul className="list-tight" aria-label="Recovery path">
              <li>Step 1: verify billing address and card security code.</li>
              <li>Step 2: retry charge manually or wait for automated retry window.</li>
              <li>Step 3: if retry fails, open Payment Recovery to update payment method.</li>
            </ul>
          </section>
        </div>
        <div className="actions">
          <Link href="/app/settings/billing/history">Billing History</Link>
          <Link href="/app/settings/billing/recovery">Payment Recovery</Link>
          <Link href="/app/onboarding/complete">Finish Onboarding</Link>
        </div>
      </div>
    </main>
  );
}
