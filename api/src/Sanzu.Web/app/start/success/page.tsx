import Link from "next/link";

export default function StartSuccessPage() {
  return (
    <main>
      <h1>Account Creation Started</h1>
      <p className="meta">Success state for start-account conversion flow.</p>
      <div className="panel">
        <p>Your workspace setup is in progress. Continue to onboarding steps from your invite link.</p>
        <p className="meta">
          Need implementation guidance now? Visit <Link href="/resources">Resources</Link>.
        </p>
      </div>
    </main>
  );
}
