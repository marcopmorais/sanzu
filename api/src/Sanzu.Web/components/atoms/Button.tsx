type ButtonVariant = "primary" | "secondary";

export function Button({
  label,
  variant = "primary",
  disabled = false
}: {
  label: string;
  variant?: ButtonVariant;
  disabled?: boolean;
}) {
  const style =
    variant === "primary"
      ? { background: "var(--brand-forest)", color: "#fff", border: "1px solid var(--brand-forest)" }
      : { background: "#f6f8f7", color: "var(--ink)", border: "1px solid var(--line)" };

  return (
    <button
      type="button"
      disabled={disabled}
      style={{
        ...style,
        borderRadius: 8,
        padding: "8px 12px",
        fontSize: 12,
        opacity: disabled ? 0.55 : 1,
        cursor: disabled ? "not-allowed" : "pointer"
      }}
    >
      {label}
    </button>
  );
}
