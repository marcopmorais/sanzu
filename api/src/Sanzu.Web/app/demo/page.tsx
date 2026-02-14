import Link from "next/link";
import { Button } from "@/components/atoms/Button";

export default function DemoPage() {
  return (
    <main>
      <h1>Request a Demo</h1>
      <p className="meta">Public conversion route for qualified demo intent.</p>
      <div className="panel">
        <p>Demo form scaffold with validation, error, and success handling paths.</p>
        <div style={{ display: "flex", gap: 8 }}>
          <Button label="Submit Demo Request" />
          <Link href="/demo/success">View Success State</Link>
        </div>
      </div>
    </main>
  );
}
