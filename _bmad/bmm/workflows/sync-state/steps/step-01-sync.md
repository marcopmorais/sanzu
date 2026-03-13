# Step 01 - Sync Current State

## Required Inputs

No external integration inputs are required.

---

## Step 1: Pull Local PDLC Status

Read these local artifacts:

- `_bmad-output/status/pdlc_state.md`
- `_bmad-output/status/current_state.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Collect:

- Current PDLC phase and runtime enforcement notes
- Active blockers and remediation actions
- Current sprint story/epic status snapshot

---

## Step 2: Pull Local UX Feedback

Read these local artifacts when present:

- `_bmad-output/design/ux-feedback.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`

Collect:

- unresolved UX review notes
- latest UX decisions impacting delivery priority

---

## Step 3: Write Current State Artifact

Create or overwrite `_bmad-output/status/current_state.md` with these sections:

- Timestamp
- PDLC Snapshot (grouped by stream)
- Blockers (from local artifacts)
- Latest UX Feedback (unresolved first)
- Next Recommended Actions (3 bullets)

If any decisions are made during the sync, add a `Decisions` section and capture them.

---

## Step 4: Local Sync Note

Append a short local sync note to the same file, including:

- Summary of key changes in this sync run
- Paths of artifacts refreshed

If any tool call fails, log the error in `_bmad-output/status/current_state.md` under `Blockers`.
