---
name: metrics-pack
description: Create metrics plan and tracking spec for the PDLC Metrics & Analytics phase.
web_bundle: false
---

# Metrics Pack Workflow

**Goal:** Produce a metrics plan and tracking spec that align product outcomes to measurable signals.

## When To Use

Use this workflow during the PDLC Metrics & Analytics phase to define product success metrics and analytics instrumentation requirements.

## Inputs (Read-Only)

- Any PRD drafts under `_bmad-output/definition/`
- Any product brief under `_bmad-output/strategy/` (if exists)
- Any UX spec under `_bmad-output/design/` (if exists)

If any inputs are missing, proceed best-effort and clearly state assumptions.

## Outputs (Write-Only)

- `_bmad-output/metrics/metrics_plan.md`
- `_bmad-output/metrics/tracking_spec.md`

Do NOT write to folders `01-*` through `10-*`.

## Steps + Involved Agents

1. **Input scan (PM + Analyst)**
   - List the files to be read from `_bmad-output/strategy/`, `_bmad-output/definition/`, `_bmad-output/design/`.
   - If any are missing, note assumptions explicitly.

2. **Metrics design (PM + Analyst)**
   - PM aligns metrics to outcomes and scope.
   - Analyst structures the KPI tree, leading/lagging indicators, and targets.

3. **Tracking specification (Analyst)**
   - Define event taxonomy, funnels, data quality checks, and dashboards.

4. **Write artifacts (PM + Analyst)**
   - Write the two output files listed below.
   - Each artifact must end with a "Promotion Instructions" section pointing to `08-Metrics-Analytics/`.

---

## Artifact A: Metrics Plan

Write `_bmad-output/metrics/metrics_plan.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - North Star metric + supporting metrics
  - KPI tree (structured)
  - Leading vs lagging indicators
  - Targets (placeholders if unknown)
  - Experiment measurement guidance
- Promotion Instructions (target: `08-Metrics-Analytics/`)

## Artifact B: Tracking Spec

Write `_bmad-output/metrics/tracking_spec.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Draft content
  - Event taxonomy (events, properties, user identifiers)
  - Critical funnels (steps + definitions)
  - Data quality checks
  - Dashboard requirements
- Promotion Instructions (target: `08-Metrics-Analytics/`)