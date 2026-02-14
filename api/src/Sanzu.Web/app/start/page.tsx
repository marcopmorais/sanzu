import Link from "next/link";
import { Button } from "@/components/atoms/Button";

export default function StartPage() {
  return (
    <main>
      <h1>Start with Sanzu</h1>
      <p className="meta">Public conversion route that starts account creation and onboarding.</p>
      <div className="panel">
        <p>Account creation form scaffold routed to tenant signup and onboarding activation.</p>
        <div style={{ display: "flex", gap: 8 }}>
          <Button label="Create Account" />
          <Link href="/start/success">View Success State</Link>
        </div>
      </div>
    </main>
  );
}
