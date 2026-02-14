import Link from "next/link";
import { OnboardingStepper } from "@/components/organisms/OnboardingStepper";

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
          </section>
          <section className="panel">
            <h2>Team Invitations</h2>
            <p className="meta">Bound to onboarding invitations contract.</p>
          </section>
        </div>
        <Link href="/app/onboarding/billing">Continue to Billing</Link>
      </div>
    </main>
  );
}
