type ButtonVariant = "primary" | "secondary";

export function Button({
  label,
  variant = "primary"
}: {
  label: string;
  variant?: ButtonVariant;
}) {
  const style =
    variant === "primary"
      ? { background: "var(--brand-forest)", color: "#fff", border: "1px solid var(--brand-forest)" }
      : { background: "#f6f8f7", color: "var(--ink)", border: "1px solid var(--line)" };

  return (
    <button type="button" style={{ ...style, borderRadius: 8, padding: "8px 12px", fontSize: 12 }}>
      {label}
    </button>
  );
}
