import Link from "next/link";

export default function HomePage() {
  return (
    <main>
      <h1>Sanzu Frontend Baseline</h1>
      <p className="meta">
        This app implements the approved Next.js 14 App Router baseline for frontend companion stories.
      </p>
      <div className="panel">
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
