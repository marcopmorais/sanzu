import Link from "next/link";

export default function TrustPage() {
  return (
    <main>
      <h1>Trust</h1>
      <p className="meta">Security, compliance, and reliability posture for agencies and families.</p>
      <div className="panel">
        <ul className="list-tight">
          <li>Tenant isolation with strict workspace boundaries.</li>
          <li>Immutable auditability for lifecycle and policy actions.</li>
          <li>Policy controls with role-safe approval workflows.</li>
        </ul>
        <p className="meta" style={{ marginTop: 8 }}>
          See legal commitments in <Link href="/legal/privacy">Privacy</Link> and{" "}
          <Link href="/legal/terms">Terms</Link>.
        </p>
      </div>
    </main>
  );
}
