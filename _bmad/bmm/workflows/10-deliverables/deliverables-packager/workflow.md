---
name: deliverables-packager
description: Package PDLC drafts into a final deliverables index and promotion checklist.
web_bundle: false
---

# Deliverables Packager Workflow

**Goal:** Inventory all draft artifacts and produce a final deliverables index and promotion checklist.

## When To Use

Use this workflow at the final PDLC packaging phase to prepare deliverables for promotion.

## Inputs (Read-Only)

- Recursively scan `_bmad-output/` for draft artifacts.

If any expected artifacts are missing, proceed best-effort and clearly state gaps.

## Outputs (Write-Only)

- `_bmad-output/deliverables/deliverables_index.md`
- `_bmad-output/deliverables/promotion_checklist.md`

Do NOT write to folders `01-*` through `10-*`.

## Steps + Involved Agents

1. **Inventory scan (Tech Writer)**
   - Recursively list artifacts under `_bmad-output/`.
   - Group by phase: strategy, discovery, opportunity, design, definition, delivery, gtm, metrics, documentation.

2. **Completeness validation (PM)**
   - Validate phase coverage and note missing artifacts.
   - Identify cross-phase dependencies and risks/gaps.

3. **Write deliverables index (Tech Writer)**
   - Produce `_bmad-output/deliverables/deliverables_index.md`.

4. **Write promotion checklist (PM + Tech Writer)**
   - Produce `_bmad-output/deliverables/promotion_checklist.md`.
   - Include sign-off placeholders.

5. **Promotion instructions**
   - Each artifact must end with a "Promotion Instructions" section pointing to `10-Deliverables/`.

---

## Artifact A: Deliverables Index

Write `_bmad-output/deliverables/deliverables_index.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Executive summary
  - Artifact list by phase
  - Status (Present / Missing / Draft)
  - Cross-phase dependency map
  - Risks or gaps
- Promotion Instructions (target: `10-Deliverables/`)

## Artifact B: Promotion Checklist

Write `_bmad-output/deliverables/promotion_checklist.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Table mapping each draft artifact → target canonical folder (01-10)
  - Required validation before promotion
  - Sign-off placeholders
- Promotion Instructions (target: `10-Deliverables/`)