# Opportunity Run - Portfolio Shaping for All Ideas (Sanzu)

Date: 2026-02-14
Phase: Opportunity
Scope: Run Opportunity shaping across the full idea set (evaluated and non-evaluated) and produce a prioritized opportunity portfolio with recommended slices.

## Inputs

- Core opportunity brief: `_bmad-output/opportunity/opportunity_brief.md`
- vNext options inventory (non-evaluated): `_bmad-output/strategy/strategy-vnext-kickoff-non-evaluated-ideas-2026-02-14.md`
- Phase 1 consolidation (mission control/admin/copilot): `_bmad-output/analysis/phase-1-run-consolidated-2026-02-14.md`
- Phase 1 CIS backlog (engineering + narrative + UX): `_bmad-output/status/phase-1-cis-party-mode-improvements-2026-02-14.md`
- Strategy addendum (mission control + copilot): `_bmad-output/strategy/strategy-addendum-mission-control-agentic-copilot-2026-02-14.md`
- Post-MVP epics (9-13): `_bmad-output/definition/epics_and_stories.md`
- Canonical mission control + copilot contracts: `_bmad-output/definition/prd.md` and:
  - `_bmad-output/definition/mission-control-reason-categories.md`
  - `_bmad-output/definition/mission-control-operational-queues.md`
  - `_bmad-output/definition/mission-control-event-taxonomy.md`
  - `_bmad-output/definition/copilot-contract.md`

## Goal

Decide, for every meaningful idea/theme:

- the opportunity hypothesis (who pays, what pain, what outcome)
- impact/feasibility/risk level
- recommendation (adopt, adopt with constraints, defer, drop)
- the smallest slice that validates value (and what evidence is required)

## Portfolio Criteria (Scoring Lens)

Interpretation:

- Impact: expected outcome improvement (time, trust, cost, retention)
- Feasibility: time-to-ship with acceptable rework risk
- Risk: trust/compliance/safety/rework risks

## Portfolio (Themes -> Recommendation)

### Theme 1: Mission Control UX (Agency + Family)

- Opportunity hypothesis:
  - Agencies pay for faster decisions, fewer mistakes, better SLA adherence.
  - Families succeed when confusion and rework decline under stress.
- Primary outcomes:
  - agency time-to-next-action
  - blocked-step recovery time
  - family first critical steps completion without rework
- Recommendation: Adopt with constraints (Post-MVP + controlled rollout)
  - Constraint: enforce detect -> explain -> act -> verify loop; no dashboard-only delivery.
- Epic mapping:
  - Epic 9 (glossary + blocked recovery) and (future) expansion of mission control shell patterns.

Impact: High
Feasibility: Medium
Risk: Medium (language + role boundaries)

### Theme 2: Platform Mission Control (Sanzu Admin)

- Opportunity hypothesis:
  - Platform operability reduces support cost, improves retention, increases trust posture.
- Primary outcomes:
  - admin time-to-diagnosis <= 60s
  - admin time-to-remediation <= 2m (audit note included)
- Recommendation: Adopt (Post-MVP core bet)
  - Rationale: current "admin controls" without queues/remediation will not be used operationally.
- Epic mapping:
  - Epic 12

Impact: High
Feasibility: Medium
Risk: Medium-High (safety/guardrails must be perfect)

### Theme 3: Drafting-First Copilot (Inside Mission Control)

- Opportunity hypothesis:
  - Drafts reduce coordination overhead and improve clarity without autonomy risk.
- Primary outcomes:
  - fewer repeated evidence-request cycles
  - improved time-to-recovery (with explainability)
- Recommendation: Adopt with constraints (Post-MVP)
  - Constraint: drafting-first, explainable, role-safe, confirm-before-send/apply (per canonical contract).
- Epic mapping:
  - Epic 13

Impact: Medium-High
Feasibility: Medium
Risk: High if contract is violated; Medium if contract is enforced

### Theme 4: Trust, Evidence, and Measurement Primitives

- Opportunity hypothesis:
  - Deterministic, reason-coded primitives are the product moat and unlock analytics and safe AI.
- Primary outcomes:
  - measurable operational reliability
  - auditability and role safety
- Recommendation: Adopt (required foundation)
- Epic mapping:
  - Epic 11 (trust telemetry) plus canonical definition specs

Impact: High
Feasibility: Medium
Risk: Medium (privacy); controllable via payload rules

### Theme 5: Agency Playbooks and Governed Defaults

- Opportunity hypothesis:
  - Agencies want repeatable handling patterns; playbooks reduce variability and training cost.
- Primary outcomes:
  - faster onboarding, fewer mistakes, more predictable case initialization
- Recommendation: Defer (Post-MVP, after mission control primitives prove value)
  - Rationale: playbooks compound once mission control loop is credible and telemetry exists.
- Epic mapping:
  - Epic 10

Impact: Medium
Feasibility: Medium
Risk: Medium (configuration complexity, governance)

### Theme 6: Compliance Export and Evidence Packaging

- Opportunity hypothesis:
  - Agencies need defensible audit/evidence exports for compliance and partner coordination.
- Primary outcomes:
  - reduced compliance overhead; fewer escalations
- Recommendation: Adopt with constraints (Post-MVP)
  - Constraint: strict redaction and role-safe export rules.
- Epic mapping:
  - Epic 11

Impact: Medium-High
Feasibility: Medium
Risk: Medium-High (data exposure)

### Theme 7: "Golden Path" Execution (End-to-End Pilot Loop)

- Opportunity hypothesis:
  - The fastest compounding value is a demoable, measurable end-to-end operating loop.
- Primary outcomes:
  - activation, time-to-first-task, predictable progression
- Recommendation: Adopt (always-on program)
  - Rationale: this is the delivery backbone that proves value and surfaces gaps early.
- Epic mapping:
  - Cross-epic (1-8 already done) plus quality gates; becomes the test bed for Epics 9-13.

Impact: High
Feasibility: High
Risk: Low

### Theme 8: Staged Auth / Security Migration Plan

- Opportunity hypothesis:
  - Pilot credibility requires a realistic security migration sequence (local header auth is insufficient for production trust).
- Primary outcomes:
  - reduced rework; improved trust and compliance readiness
- Recommendation: Adopt (as an explicit plan + implementation sequencing)
- Epic mapping:
  - Cross-cutting (architecture/delivery); not a single epic in the current post-MVP set.

Impact: High
Feasibility: Medium
Risk: Medium (integration and scope creep)

## Prioritized Opportunity Slices (What To Do Next)

1. Slice A: Platform operability (Admin mission control)
- Epics: 12
- Validation: admin diagnose/remediate timings + safety/guardrail UX works

2. Slice B: Case mission control recovery (Family + Agency)
- Epics: 9 + 11 telemetry subset
- Validation: next-action and recovery improvements + plain-language effectiveness

3. Slice C: Copilot drafting-first
- Epics: 13 (after Slice A/B primitives are in place)
- Validation: draft acceptance/edit rates, explainability preference, no trust regressions

## Evidence Required Before Broad Rollout

- Timed usability results for agency/family/admin scenarios
- Confirmed reason-category vocabulary and plain-language mapping (family-safe)
- Confirmed admin action guardrails (impact preview + audit note + verification)
- Privacy review for telemetry and exports

## Output

This run produces:

- a portfolio recommendation for all major ideas/themes
- a prioritized slice plan that connects directly to Post-MVP Epics 9-13

