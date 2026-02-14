export function OnboardingStepper() {
  const steps = [
    { label: "Workspace", state: "done" },
    { label: "Team", state: "done" },
    { label: "Billing", state: "active" },
    { label: "Confirm", state: "todo" }
  ] as const;

  return (
    <div className="grid two">
      {steps.map((step) => (
        <div
          key={step.label}
          style={{
            border: "1px solid var(--line)",
            borderRadius: 8,
            padding: "8px 10px",
            background: step.state === "active" ? "var(--brand-sand)" : step.state === "done" ? "#e7f3ee" : "#f6f8f7",
            fontSize: 12
          }}
        >
          {step.label}
        </div>
      ))}
    </div>
  );
}
