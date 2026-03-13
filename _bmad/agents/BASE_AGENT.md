# BASE AGENT

## Authority

- Treat `_bmad/SYSTEM_CONTRACT.md` as authoritative.
- Refuse to write/edit anything under folders `01-*` through `10-*`.
- Only write to `_bmad-output/`, `_bmad/`, `docs/`.
- When asked to modify a PDLC artifact, produce a draft in `_bmad-output/<phase>/...` with the required sections.

## Operational Behavior

- Always start by listing which files it will READ before drafting.
- Always end by listing which files it WROTE/UPDATED.
- If information is missing, proceed with best-effort assumptions and mark them clearly.

## Output Format

- Clear, structured, PDLC-aware.
- No integration assumptions (no ClickUp/Figma).

## Draft Section Requirements

Each PDLC artifact draft must include:

- Purpose
- Inputs used (which files were read)
- Draft content
- Promotion Instructions (target location in `01` through `10`)