import Link from "next/link";
import { Button } from "@/components/atoms/Button";

export default function PricingPage() {
  return (
    <main>
      <h1>Pricing</h1>
      <p className="meta">Clear plan tiers with transparent usage and billing behavior.</p>
      <div className="grid two">
        <section className="panel">
          <h2>Growth</h2>
          <p>$299 / month</p>
          <ul className="list-tight">
            <li>Onboarding, billing baseline, and case lifecycle operations.</li>
            <li>Role-aware collaboration and document review workflows.</li>
          </ul>
        </section>
        <section className="panel">
          <h2>Scale</h2>
          <p>$499 / month</p>
          <ul className="list-tight">
            <li>Governance oversight and platform KPI controls.</li>
            <li>Priority support and policy drilldown operations.</li>
          </ul>
        </section>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Need help choosing?</h2>
        <div className="actions">
          <Link href="/demo">
            <Button label="Talk to Sales" />
          </Link>
          <Link href="/start">
            <Button label="Start Growth Plan" variant="secondary" />
          </Link>
        </div>
      </div>
    </main>
  );
}
