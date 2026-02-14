export function StatusBanner({ kind, text }: { kind: "ok" | "warn"; text: string }) {
  const palette =
    kind === "ok"
      ? { bg: "#e8f5ee", border: "#b7d7ca", color: "var(--ok)" }
      : { bg: "#fff1e8", border: "#efc8b2", color: "var(--warn)" };

  return (
    <p
      aria-live="polite"
      style={{
        margin: 0,
        padding: "9px 10px",
        borderRadius: 8,
        background: palette.bg,
        border: `1px solid ${palette.border}`,
        color: palette.color,
        fontSize: 12
      }}
    >
      {text}
    </p>
  );
}
