import Link from "next/link";
import { OnboardingStepper } from "@/components/organisms/OnboardingStepper";
import { Button } from "@/components/atoms/Button";

export default function OnboardingPage() {
  return (
    <main>
      <h1>Onboarding Workspace</h1>
      <p className="meta">Story 1.7 route: onboarding profile, defaults, and invitations.</p>
      <div className="panel grid">
        <OnboardingStepper />
        <div className="grid two">
          <section className="panel">
            <h2>Agency Profile</h2>
            <p className="meta">Bound to onboarding profile/defaults contracts.</p>
            <div className="grid" aria-label="Agency profile fields">
              <label>
                Agency name
                <input defaultValue="Horizon Family Partners" style={{ width: "100%", marginTop: 6, padding: 8 }} />
              </label>
              <label>
                Billing contact email
                <input
                  defaultValue="billing@horizonfp.com"
                  style={{ width: "100%", marginTop: 6, padding: 8 }}
                />
              </label>
            </div>
          </section>
          <section className="panel">
            <h2>Team Invitations</h2>
            <p className="meta">Bound to onboarding invitations contract.</p>
            <div style={{ display: "flex", gap: 8 }}>
              <Button label="Invite Team Member" variant="secondary" />
              <Button label="Validate Invitations" />
            </div>
          </section>
        </div>
        <Link href="/app/onboarding/billing">Continue to Billing</Link>
      </div>
    </main>
  );
}
