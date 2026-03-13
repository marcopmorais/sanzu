---
name: promote-drafts
description: Plan and (optionally) apply promotion of approved drafts into canonical PDLC folders.
web_bundle: false
apply: false
---

# Promote Drafts Workflow

**Goal:** Build a promotion plan for approved drafts and optionally apply it when explicitly requested.

## When To Use

Use this workflow after deliverables are prepared and approved, to plan (and optionally apply) promotion into canonical PDLC folders.

## Inputs (Read-Only)

- `_bmad-output/deliverables/promotion_checklist.md`
- `_bmad-output/deliverables/deliverables_index.md`

## Outputs (Write-Only)

- `_bmad-output/deliverables/promotion_plan.md`

Default mode is DRY RUN. Do NOT write to folders `01-*` through `10-*` unless `apply=true` and the user explicitly requests promotion.

## Steps + Involved Agents

1. **Input review (PM + Tech Writer)**
   - Read the promotion checklist and deliverables index.
   - Identify approved drafts eligible for promotion.

2. **Promotion planning (Tech Writer)**
   - Build the promotion plan with source drafts, target canonical paths, and actions.

3. **Validation and sign-off logic (PM)**
   - Confirm required approvals are present (placeholders allowed).
   - If approvals are missing, keep DRY RUN and note gaps.

4. **Optional apply (PM)**
   - Only if `apply=true` AND the user explicitly requests promotion:
     - Perform only copy/create operations explicitly listed in the promotion plan.
     - Do not overwrite existing canonical files; if a target exists, create `<name>_DRAFT.md` instead.

---

## Output Artifact: Promotion Plan

Write `_bmad-output/deliverables/promotion_plan.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - List of source draft files
  - Target canonical path in `01-*` through `10-*`
  - Action: copy / merge / create new
  - Required approvals checklist (placeholders)
- Promotion Instructions (this plan remains in `_bmad-output/`)