---
name: pdlc-master
description: Orchestrate PDLC status, detect gaps, and recommend next workflows.
web_bundle: false
allow_auto_delegate: false
---

# PDLC Master Orchestration Workflow

**Goal:** Determine PDLC maturity from existing drafts and recommend the next workflow.

## When To Use

Use this workflow to assess PDLC progress, detect gaps, and decide the next phase/workflow.

## Inputs (Read-Only)

- Recursively scan `_bmad-output/` for artifacts by phase:
  - strategy/
  - discovery/
  - opportunity/
  - design/
  - definition/
  - delivery/
  - gtm/
  - metrics/
  - documentation/
  - deliverables/

If any expected artifacts are missing, proceed best-effort and clearly state gaps.

## Outputs (Write-Only)

- `_bmad-output/status/pdlc_state.md`

Do NOT write to folders `01-*` through `10-*`.
Do NOT call ClickUp tools or perform ClickUp API actions in this workflow.
Use local artifacts under `_bmad-output/` as the operational source while ClickUp remains disabled.

## Gate Rules

Load and apply gate rules from `_bmad/pdlc_gates.yaml` when selecting the recommended next workflow.

## Steps + Agents Involved

1. **Inventory scan (BMad Master + Analyst)**
   - List all artifacts under `_bmad-output/` grouped by phase.

2. **Gap detection (Analyst)**
   - Identify missing phases or key artifacts by phase.

3. **Phase validation (PM)**
   - Determine current PDLC phase and maturity state.

4. **Recommendation (BMad Master)**
   - Apply gate rules from `_bmad/pdlc_gates.yaml`.
   - If a gate fails, do NOT recommend moving forward. Recommend the workflow that generates the missing required artifacts.
   - Otherwise, recommend the next workflow with an explicit file path.
   - If `allow_auto_delegate: true`, invoke the recommended workflow. Otherwise, only suggest.

5. **CIS review phase (Review-only)**
   - Run this phase only after artifact scan and gate check are complete.
   - Use these agents in review-only mode:
     - innovation strategist
     - design thinking coach
     - creative problem solver
     - storyteller
   - Each agent reviews current artifacts (read-only) and provides structured critique:
     - Strategic coherence
     - Problem clarity
     - Structural gaps
     - Narrative consistency
     - Risk flags
   - Do NOT allow CIS agents to generate or modify any artifacts in this phase.
   - Consolidate CIS output into:
     - Strategic Risk Level (Low/Medium/High)
     - Key Observations
     - Critical Gaps
     - Recommended strategic adjustment

6. **Write status report (BMad Master)**
   - Write `_bmad-output/status/pdlc_state.md` using the required sections below.

---

## PDLC Maturity States

Choose the best match:

- Missing Strategy
- In Discovery
- In Definition
- Ready for Delivery
- Ready for GTM
- In Metrics loop
- PDLC Complete

---

## Output Artifact: PDLC State

Write `_bmad-output/status/pdlc_state.md` with these sections:

- Purpose
- Inputs used (which files were read)
- Current PDLC phase
- Missing artifacts by phase
- Gate check results (pass/fail)
- Missing required artifacts list
- Recommended remediation workflow
- Recommended next workflow (explicit file path)
- CIS Strategic Review
  - Strategic Risk Level (Low/Medium/High)
  - Key Observations
  - Critical Gaps
  - Recommended strategic adjustment
- Risk assessment (if skipping detected)
- Promotion Instructions (status file remains in `_bmad-output/`)
