import { StatusBanner } from "@/components/molecules/StatusBanner";

export default function OnboardingCompletePage() {
  return (
    <main>
      <h1>Onboarding Complete</h1>
      <p className="meta">Story 1.7 success route after profile, billing, and activation steps.</p>
      <div className="panel">
        <StatusBanner kind="ok" text="Tenant setup and billing activation completed successfully." />
      </div>
    </main>
  );
}
