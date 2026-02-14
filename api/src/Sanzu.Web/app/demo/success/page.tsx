import Link from "next/link";

export default function DemoSuccessPage() {
  return (
    <main>
      <h1>Demo Request Received</h1>
      <p className="meta">Success state for demo conversion flow.</p>
      <div className="panel">
        <p>We received your request and will contact you with next steps within one business day.</p>
        <p className="meta">
          Next: check your inbox for scheduling options or return to <Link href="/pricing">Pricing</Link>.
        </p>
      </div>
    </main>
  );
}
