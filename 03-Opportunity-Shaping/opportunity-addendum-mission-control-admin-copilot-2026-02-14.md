# Opportunity Addendum - Mission Control Platform (Admin + Case) + Drafting Copilot

Date: 2026-02-14
Phase: Opportunity
Scope: Shape the Post-MVP opportunity for mission control primitives, platform mission control, and drafting-first copilot.

## Purpose

Translate Strategy and non-evaluated ideas into an Opportunity framing that is:

- customer and buyer anchored
- measurable (operational outcomes)
- risk-aware (trust, safety, compliance)

This addendum does not replace the core Opportunity brief; it extends it for the vNext direction.

## Inputs

- Strategy addendum: `_bmad-output/strategy/strategy-addendum-mission-control-agentic-copilot-2026-02-14.md`
- vNext options inventory: `_bmad-output/strategy/strategy-vnext-kickoff-non-evaluated-ideas-2026-02-14.md`
- Phase 1 consolidation: `_bmad-output/analysis/phase-1-run-consolidated-2026-02-14.md`
- Definition requirements (Post-MVP): `_bmad-output/definition/prd.md` (FR64-FR74)
- Epic map: `_bmad-output/definition/epics_and_stories.md` (Epics 9-13)

## Opportunity Statement (vNext)

Sanzu can expand from "workflow product" into "mission control for bereavement operations" by:

- making exceptions, blocked states, and next actions operationally obvious (agency + family)
- enabling a platform operator (Sanzu Admin) to run the platform via fleet + queues + remediation loops
- adding a drafting-first copilot that reduces coordination overhead without compromising trust

## Who Pays and Why (Buyer/ICP)

Primary buyer:

- Funeral agencies (B2B), paying for reduced coordination overhead, fewer mistakes, better deadline adherence, and faster case throughput.

Internal operator persona (drives retention and supportability):

- Sanzu Admin (platform operator) pays off via:
  - lower support cost
  - fewer tenant incidents
  - faster remediation
  - improved trust posture

Secondary beneficiary (not buyer):

- Families, paying in effort and stress; product succeeds when it reduces confusion and rework.

## Pain and Value Hypotheses

### Agency (Case Mission Control)

Pain:

- "What do we do next?" is not consistently obvious; blockers create back-and-forth and SLA risk.

Value hypothesis:

- Cockpit defaults + reason->action recovery reduce time-to-next-action and blocked recovery time.

Target outcomes:

- time-to-next-action after opening a case <= 30 seconds
- blocked-step resolution time improves >= 20% in pilot cohort

### Family (Guided Mode)

Pain:

- low cognitive bandwidth under stress; jargon and sequencing cause rework and escalations.

Value hypothesis:

- plain language + guided recovery reduces confusion and increases first critical step completion.

Target outcomes:

- early critical step completion without rework >= 85% (scenario-based)

### Sanzu Admin (Platform Mission Control)

Pain:

- platform operations are fragmented into settings, tickets, and ad hoc diagnostics; hard to find "tenants in trouble" and remediate safely.

Value hypothesis:

- fleet + queues + event stream + closed-loop remediation reduces incident duration and improves platform governance.

Target outcomes:

- time-to-diagnosis (find tenant in trouble + why) <= 60 seconds
- time-to-remediation for standard action (with audit note) <= 2 minutes

### Copilot (Drafting-First)

Pain:

- coordination overhead (messages, evidence requests, recovery checklists) consumes time and causes miscommunication.

Value hypothesis:

- drafting-first copilot reduces time and improves clarity without introducing trust regressions.

Target outcomes:

- reduced back-and-forth cycles for evidence requests (proxy via fewer repeated requests)
- stable or improved trust/confidence ratings vs manual drafting

## Differentiation (Why This Is Not "More Dashboards")

Sanzu differentiates by building mission control primitives that others do not operationalize:

- stable reason categories for blocked/exceptions (canonical, aggregatable)
- "why" drilldown that is role-safe and evidence-aware
- safe actions with impact preview + audit note + verification loop
- platform queues that map to remediation playbooks

Canonical contracts:

- Reason categories: `_bmad-output/definition/mission-control-reason-categories.md`
- Operational queues: `_bmad-output/definition/mission-control-operational-queues.md`
- Event taxonomy: `_bmad-output/definition/mission-control-event-taxonomy.md`
- Copilot contract: `_bmad-output/definition/copilot-contract.md`

## Key Risks (Opportunity Shaping)

- Trust regression: copilot or admin actions feel unsafe or opaque.
  - Mitigation: drafting-first, explainability, confirmation gates, audit notes, verification.
- Vocabulary mismatch: "reason codes" confuse family users.
  - Mitigation: plain language first; codes optional and internal.
- Operational overbuild: building dashboards without action loops.
  - Mitigation: enforce detect->explain->act->verify on every surface.
- Data sensitivity: leaking private family content into admin surfaces or telemetry.
  - Mitigation: role-safe payloads; aggregated telemetry; strict filtering.

## Recommended Opportunity Focus (Post-MVP Slice)

The highest-value slice to validate and then build is:

1. Platform mission control (Epics 12)
- fleet posture
- admin queues
- event stream drilldown
- closed-loop remediation

2. Copilot drafting-first (Epic 13)
- evidence request drafts + checklists
- recovery plan drafts + explain why

Rationale:

- Increases platform operability and reduces support cost (admin)
- Reduces coordination overhead (agency)
- Builds a safe path to "agentic" without autonomous mutations

## Success Signals (Opportunity Exit)

- Pilot: measurable improvements in time-to-next-action, blocked recovery time, and admin diagnosis/remediation times.
- Operational: fewer escalations per tenant, lower mean time to resolution for standard incidents.
- Trust: no increase in compliance incidents; stable or improved user confidence.

## Promotion Instructions

- This artifact remains in `_bmad-output/opportunity/` until formal promotion is approved.

