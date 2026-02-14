import Link from "next/link";
import { Button } from "@/components/atoms/Button";

export default function HomePage() {
  return (
    <main>
      <div className="hero">
        <h1>Sanzu</h1>
        <p className="meta">Trusted case workflow platform for agencies and families.</p>
        <div className="actions">
          <Link href="/demo">
            <Button label="Book Demo" />
          </Link>
          <Link href="/start">
            <Button label="Start Onboarding" variant="secondary" />
          </Link>
        </div>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Explore</h2>
        <div className="actions">
          <Link href="/trust">Trust</Link>
          <Link href="/pricing">Pricing</Link>
          <Link href="/resources">Resources</Link>
          <Link href="/legal/privacy">Privacy</Link>
          <Link href="/legal/terms">Terms</Link>
        </div>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
        <h2>Frontend Story Routes</h2>
        <ul>
          <li>
            <Link href="/app/onboarding">Story 1.7 Onboarding</Link>
          </li>
          <li>
            <Link href="/app/onboarding/billing">Story 1.7 Billing Activation</Link>
          </li>
          <li>
            <Link href="/app/settings/billing/history">Billing History</Link>
          </li>
          <li>
            <Link href="/app/settings/billing/recovery">Payment Recovery</Link>
          </li>
        </ul>
      </div>
    </main>
  );
}
