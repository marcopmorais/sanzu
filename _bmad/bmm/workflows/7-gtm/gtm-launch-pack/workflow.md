---
name: gtm-launch-pack
description: Create GTM launch artifacts for the PDLC GTM/Launch phase.
web_bundle: false
---

# GTM Launch Pack Workflow

**Goal:** Produce a launch plan, comms brief, and positioning narrative for GTM/Launch.

## When To Use

Use this workflow during the PDLC GTM/Launch phase to align rollout, communications, and positioning.

## Inputs (Read-Only)

- PRD draft: `_bmad-output/definition/prd.md` (if exists)
- UX spec: `_bmad-output/design/ux-spec.md` (if exists)
- Architecture/implementation notes: `_bmad-output/delivery/` (if exists)

If any inputs are missing, proceed best-effort and clearly state assumptions.

## Outputs (Write-Only)

- `_bmad-output/gtm/launch_plan.md`
- `_bmad-output/gtm/comms_brief.md`
- `_bmad-output/gtm/positioning_narrative.md`

Do NOT write to folders `01-*` through `10-*`.

## Steps + Involved Agents

1. **Input scan (PM)**
   - List the files to be read from `_bmad-output/definition/`, `_bmad-output/design/`, `_bmad-output/delivery/`.
   - If any are missing, note assumptions explicitly.

2. **Rollout and scope alignment (PM)**
   - Define target segments, rollout approach, risks, mitigations, timeline, and ownership placeholders.

3. **Comms structure (Presentation Master)**
   - Structure the comms brief for audiences, channels, cadence, FAQ, and support escalation.

4. **Positioning narrative (Storyteller)**
   - Draft the narrative arc and pitch variants with placeholders for proof points.

5. **Write artifacts (PM + Storyteller + Presentation Master)**
   - Write the three output files listed below.
   - Each artifact must end with a "Promotion Instructions" section pointing to `07-GTM-Launch/`.

---

## Artifact A: Launch Plan

Write `_bmad-output/gtm/launch_plan.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Target segments + rollout approach (phased/beta/general)
  - Readiness checklist (product, ops, support, legal/privacy placeholders)
  - Risks + mitigations
  - Timeline with milestones
  - Ownership/RACI (placeholders if unknown)
- Promotion Instructions (target: `07-GTM-Launch/`)

## Artifact B: Comms Brief

Write `_bmad-output/gtm/comms_brief.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Key messages by audience (users, partners, internal)
  - Channels + cadence
  - FAQ outline
  - Support + escalation notes
- Promotion Instructions (target: `07-GTM-Launch/`)

## Artifact C: Positioning Narrative

Write `_bmad-output/gtm/positioning_narrative.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Problem story
  - Why now
  - Differentiation
  - Proof points (placeholders if unknown)
  - One-liner and 30-second pitch
- Promotion Instructions (target: `07-GTM-Launch/`)