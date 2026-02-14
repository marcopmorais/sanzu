import Link from "next/link";

export default function HomePage() {
  return (
    <main>
      <h1>Sanzu</h1>
      <p className="meta">Trusted case workflow platform for agencies and families.</p>
      <div className="panel">
        <ul>
          <li>
            <Link href="/trust">Trust</Link>
          </li>
          <li>
            <Link href="/pricing">Pricing</Link>
          </li>
          <li>
            <Link href="/resources">Resources</Link>
          </li>
          <li>
            <Link href="/demo">Book Demo</Link>
          </li>
          <li>
            <Link href="/start">Start Now</Link>
          </li>
        </ul>
      </div>
      <div className="panel" style={{ marginTop: 14 }}>
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
