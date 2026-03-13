import Link from "next/link";
import { OnboardingStepper } from "@/components/organisms/OnboardingStepper";
import { Button } from "@/components/atoms/Button";

export default function OnboardingPage() {
  return (
    <main>
      <h1>Onboarding Workspace</h1>
      <p className="meta">Story 1.7 route: onboarding profile, defaults, and invitations.</p>
      <div className="hero">
        <h2>Tenant setup progress: 2 of 4 complete</h2>
        <p className="meta">Complete profile and invitations before billing activation is unlocked.</p>
      </div>
      <div className="panel grid" style={{ marginTop: 14 }}>
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
              <p className="meta">Validation: agency legal name and billing email must be present to continue.</p>
            </div>
          </section>
          <section className="panel">
            <h2>Team Invitations</h2>
            <p className="meta">Bound to onboarding invitations contract.</p>
            <div className="actions">
              <Button label="Invite Team Member" variant="secondary" />
              <Button label="Validate Invitations" />
            </div>
            <ul className="list-tight" aria-label="Invitation checks">
              <li>At least one Process Manager invite is required.</li>
              <li>Duplicate invite emails are blocked before submit.</li>
            </ul>
          </section>
        </div>
        <Link href="/app/onboarding/billing">Continue to Billing</Link>
      </div>
    </main>
  );
}
