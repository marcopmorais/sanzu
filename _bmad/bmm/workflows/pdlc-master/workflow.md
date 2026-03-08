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

Recursively scan `_bmad-output/` for artifacts by phase:

| Folder | Phase |
|--------|-------|
| `0-portfolio/` | Phase 0 — Portfolio Framing |
| `1-strategy/` | Phase 1 — Strategy |
| `2-discovery/` | Phase 2 — Discovery |
| `2-discovery/opportunity-solution-trees/` | Phase 2 — OSTs |
| `planning-artifacts/prd/` | Phase 3 — Definition (PRD) |
| `planning-artifacts/epics/` | Phase 3 — Definition (Epics) |
| `planning-artifacts/okrs/` | Phase 3 — OKRs |
| `planning-artifacts/roadmaps/` | Phase 3 — Roadmaps |
| `design/` | Phase 4 — Design (UX) |
| `5-architecture/` | Phase 5 — Architecture Review |
| `5-architecture/deployment-design.md` | Phase 5 — Deployment Design (required for Phase 5.5 entry) |
| `5.5-cicd/` | Phase 5.5 — CI/CD Scaffold |
| `5.5-cicd/pipeline-baseline.md` | Phase 5.5 — Pipeline green baseline (required for DoR) |
| `5.5-cicd/quality-gate-spec.md` | Phase 5.5 — Quality gate definitions |
| `implementation-artifacts/` | Phase 6 — Delivery (Build) |
| `testing/` | Phase 6 — Delivery (QA) |
| `7-release/` | Phase 7 — Release |
| `8-growth/` | Phase 8 — Growth & Experimentation |
| `9-post-release/` | Phase 9 — Post-Release Learning |
| `10-lifecycle/` | Phase 10 — Lifecycle Management |
| `content/` | Supporting content artifacts |
| `status/` | Operational status |

If any expected artifacts are missing, proceed best-effort and clearly state gaps.

## Outputs (Write-Only)

- `_bmad-output/status/pdlc_state.md`

Do NOT write to numbered phase folders.
Do NOT call ClickUp or Figma tools in this workflow.
Use local artifacts under `_bmad-output/` as the operational source of truth.

## Gate Rules

Load and apply gate rules from `_bmad/pdlc_gates.yaml` when selecting the recommended next workflow.

Each gate specifies:
- `required_artifacts` — glob patterns; at least one real file must exist
- `required_fields` — checklist items that must appear in artifact content
- `on_fail.recommended_workflow` — workflow to run if gate fails
- `on_pass.recommended_workflow` — workflow to advance to next phase

If a gate fails, do NOT recommend moving forward. Recommend the workflow that generates the missing required artifacts.

## Steps + Agents Involved

1. **Inventory scan**
   - List all files under `_bmad-output/` grouped by phase folder.
   - Skip `.gitkeep` files — they indicate empty phases.
   - Record: which phases have real artifacts, which are empty.

2. **Gap detection**
   - Identify missing phases or key artifacts by phase.
   - For each present phase, check against `required_artifacts` patterns in `_bmad/pdlc_gates.yaml`.
   - Flag any phase where artifacts exist but `required_fields` checklist items cannot be confirmed.

3. **Phase validation**
   - Determine current PDLC phase using the Maturity States table below.
   - A phase is "complete" when its gate passes (all `required_artifacts` present + all `required_fields` confirmed).
   - A phase is "in progress" when some artifacts exist but gate has not passed.

4. **Gate check and recommendation**
   - Apply gate rules from `_bmad/pdlc_gates.yaml` for the current and next phase.
   - If gate fails → recommend `on_fail.recommended_workflow`.
   - If gate passes → recommend `on_pass.recommended_workflow`.
   - If `allow_auto_delegate: true`, invoke the recommended workflow. Otherwise, only suggest.

5. **CIS strategic review phase (Review-only)**
   - Run this phase only after artifact scan and gate check are complete.
   - Use these CIS agents in review-only mode (located in `_bmad/cis/agents/`):
     - `innovation-strategist.md` — strategic coherence and bold bets
     - `design-thinking-coach.md` — problem clarity and human-centered gaps
     - `creative-problem-solver.md` — structural gaps and alternative framings
     - `storyteller.md` — narrative consistency and communication risk
   - Each agent reviews current artifacts (read-only) and provides structured critique:
     - Strategic coherence
     - Problem clarity
     - Structural gaps
     - Narrative consistency
     - Risk flags
   - Do NOT allow CIS agents to generate or modify any artifacts in this phase.
   - Consolidate CIS output into:
     - Strategic Risk Level (Low / Medium / High)
     - Key Observations (≤5 bullet points)
     - Critical Gaps (must fix before advancing)
     - Recommended strategic adjustment

6. **Write status report**
   - Write `_bmad-output/status/pdlc_state.md` using the required sections below.

---

## PDLC Maturity States

Map current artifacts to the highest phase where the gate passes:

| Maturity State | Gate Condition |
|---|---|
| Phase 0 — Not Started | No real artifacts in any phase folder |
| Phase 0 — Portfolio Framing | `0-portfolio/` has artifacts |
| Phase 1 — Strategy | `1-strategy/` has artifacts |
| Phase 2 — Discovery | `2-discovery/` has artifacts including problem statement |
| Phase 3 — Definition | `planning-artifacts/prd/` AND `planning-artifacts/epics/` have artifacts |
| Phase 4 — Design | `design/` has artifacts AND UX_Maturity field present |
| Phase 5 — Architecture Review | `5-architecture/` has artifacts AND Architecture_Approved confirmed AND Deployment_Design_Approved confirmed |
| Phase 5.5 — CI/CD Scaffold | `5.5-cicd/pipeline-baseline.md` exists AND CI_Pipeline_Green = true confirmed |
| Phase 6 — Implementation Ready (DoR pass) | All DoR conditions confirmed true, including CI_Pipeline_Green = true |
| Phase 6 — In Build | `implementation-artifacts/` has story files |
| Phase 6 — In QA | `testing/` has test artifacts |
| Phase 7 — Release Ready (DoD pass) | All DoD conditions confirmed true |
| Phase 7 — Released | `7-release/` has release plan with baseline metric |
| Phase 8 — Growth | `8-growth/` has experiment report |
| Phase 9 — Post-Release Learning | `9-post-release/` has ROI recalculation |
| Phase 10 — Lifecycle Management | `10-lifecycle/` has closure record |
| PDLC Complete | All phase folders populated with gate-passing artifacts |

---

## Output Artifact: PDLC State

Write `_bmad-output/status/pdlc_state.md` with these sections:

```markdown
# PDLC State Report

**Generated:** {timestamp}
**Workflow:** pdlc-master

## Inputs Used
- List every file read during this run

## Current PDLC Phase
- Maturity state (from table above)
- Highest gate passed

## Phase Inventory

| Phase | Folder | Status | Artifact Count |
|-------|--------|--------|----------------|
| 0 | 0-portfolio/ | ✅ Present / ⚠️ Empty / ❌ Missing | N |
| ... | ... | ... | ... |

## Gate Check Results

For each gate in _bmad/pdlc_gates.yaml:
| Gate | Status | Missing Required Artifacts | Missing Required Fields |
|------|--------|---------------------------|------------------------|
| phase_0_portfolio | ✅ Pass / ❌ Fail | ... | ... |

## Missing Required Artifacts
- Bulleted list of all required_artifacts patterns with no matching files

## Recommended Remediation Workflow
- If current gate fails: explicit file path from on_fail.recommended_workflow
- With reasoning

## Recommended Next Workflow
- After remediation: explicit file path from on_pass.recommended_workflow of next gate

## CIS Strategic Review
- **Strategic Risk Level:** Low / Medium / High
- **Key Observations:**
  - (≤5 bullets)
- **Critical Gaps:**
  - (must fix before advancing)
- **Recommended Strategic Adjustment:**
  - (1–3 sentences)

## Risk Assessment
- Note any detected phase skipping
- Note any DoR/DoD conditions that could not be confirmed

## Promotion Instructions
- Status file remains in `_bmad-output/status/`
- Do not move this file to phase-specific folders
```
