import { Button } from "@/components/atoms/Button";
import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function BillingRecoveryPage() {
  return (
    <main>
      <h1>Payment Recovery</h1>
      <p className="meta">Story 1.7 recovery route for failed payments.</p>
      <div className="panel grid">
        <StatusBanner kind="warn" text="Last payment failed. Retry and reminder schedule is active." />
        <div style={{ display: "flex", gap: 8 }}>
          <Button label="Retry Payment" />
          <Button label="View Recovery Timeline" variant="secondary" />
        </div>
      </div>
    </main>
  );
}
