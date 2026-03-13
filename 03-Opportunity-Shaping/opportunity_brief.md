# Opportunity Brief

## Purpose
Capture the highest-value opportunity hypotheses for Sanzu and define a prioritized opportunity direction that bridges discovery evidence into design and delivery planning.

## Inputs used (which files were read)
- `_bmad-output/strategy/product_brief.md`
- `_bmad-output/discovery/market_research.md`
- `_bmad-output/discovery/portugal_obito_process_deep_dive_input.md`
- `_bmad-output/definition/prd.md`
- `_bmad-output/metrics/metrics_plan.md`
- `_bmad-output/metrics/tracking_spec.md`
- `_bmad-output/status/project_context.md`

## Draft content

### Opportunity framing
Sanzu has a strong opportunity to become the default agency-led operating layer for post-loss bureaucracy in Portugal by reducing coordination overhead, improving completion reliability, and increasing confidence for families under stress.

### Prioritized opportunity areas

1. **Agency-Centered Case Orchestration (Primary)**
- Problem: Agencies lose operational time to fragmented coordination and repeated follow-up.
- Opportunity: Standardize case execution with deterministic workflows, role-safe collaboration, and auditability.
- Why now: Discovery indicates recurring annual demand and clear process fragmentation.

2. **Family Clarity and Guided Completion (Primary)**
- Problem: Families struggle with "what to do next" under emotional load.
- Opportunity: Provide clear next actions, dependency-aware progression, and transparent status views.
- Why now: User pain evidence and early success metrics strongly support this as a core value driver.

3. **Document and Template Automation Layer (Secondary)**
- Problem: Document handling and outbound communication are high-friction, error-prone, and repetitive.
- Opportunity: Use template-driven outputs and structured evidence handling to reduce rework.
- Why now: Fits deterministic-first scope and improves delivery quality without requiring risky over-automation.

4. **Commercial Expansion by Usage Tier (Secondary)**
- Problem: Agency willingness-to-pay varies by volume and operational maturity.
- Opportunity: Align pricing and packaging to case volume and operational value realization.
- Why now: PRD pricing model is defined and ready for pilot validation.

### Opportunity prioritization matrix (current)

- **High impact / High feasibility:** Agency-centered case orchestration, family clarity flows.
- **High impact / Medium feasibility:** Document/template automation depth.
- **Medium impact / Medium feasibility:** Advanced optimization and recommendation features.
- **Medium impact / Low feasibility (defer):** Broad cross-institution automated submissions in MVP.

### Recommended opportunity focus for next phase

- Maintain a two-track focus:
  - **Track A (must-win):** Agency orchestration + family guidance workflows.
  - **Track B (enablement):** Document/template reliability and collaboration transparency.

### Opportunity validation signals

- Agency activation within 14 days of onboarding.
- Measurable reduction in coordination/rework compared to baseline process.
- Family confidence and deadline adherence improvements.
- Early retention and referral behavior from pilot agencies.

### Ideas to Evaluate

1. **Document intelligence during upload**
- Idea: When users upload documents (utilities invoices, insurance documents, banking reports, and related artifacts), Sanzu should extract step-relevant information and prefill/process matching workflow fields.
- Evaluation focus: extraction accuracy, confidence thresholds, human review flow, and legal/privacy handling for sensitive data.

2. **Process email naming convention**
- Idea: Define process mailbox format as `processslug@sanzy.ai` for each process/customer context.
- Evaluation focus: uniqueness rules, collisions, privacy implications of naming, and mailbox lifecycle management.

3. **New persona: Sanzu Administrators (super-users)**
- Idea: Add a platform-operator persona representing internal Sanzu staff who manage tenants, support escalations, policy controls, and system governance.
- Evaluation focus: role boundaries vs agency roles, required capabilities, audit controls, and support workflows.

4. **Missing use case: Customer account (Agency) creation**
- Idea: Explicitly model agency account creation as a first-class journey/use case (signup, tenant bootstrap, billing setup, and initial configuration).
- Evaluation focus: onboarding friction, identity/verification flow, billing activation, and first-value time.

5. **Sanzu administrative outcome and KPI evaluation**
- Idea: Sanzu administrators need dedicated capabilities to evaluate operational outcomes and track success KPIs defined for pilot and scale decisions.
- Evaluation focus: role-scoped dashboards, KPI drill-downs, cohort analysis, and escalation triggers for underperforming metrics.

6. **In-product inbox for process emails**
- Idea: Inside Sanzu, users can view emails sent and received for each process, with inbox-style visibility.
- Evaluation focus: message threading model, access control per case/role, data retention, searchability, and audit traceability.

7. **Public website for non-authenticated users**
- Idea: Launch a public Sanzu website for visitors who are not logged in, covering product explanation, trust/compliance positioning, pricing, and conversion paths to demo/signup.
- Evaluation focus: message-market fit, conversion funnel design, SEO discoverability, legal/compliance disclosures, analytics instrumentation, and handoff into account onboarding.

### Evaluation status (Reassessment with legal + metrics inputs)

1. **Document intelligence during upload**
- Status: **Adopt with constraints (controlled rollout)**.
- Rationale: Legal/process workflows are document-heavy and high-value for assisted extraction, but automation must remain human-confirmed with auditable acceptance/override.

2. **Process email naming convention**
- Status: **Adopt (policy-controlled)**.
- Rationale: Use the canonical process alias format `processslug@sanzy.ai`; enforce uniqueness and lifecycle governance while avoiding personal-name exposure.

3. **Sanzu Administrators persona**
- Status: **Adopt**.
- Rationale: Required for tenant governance, escalated support, platform-level operations, and compliance-sensitive issue handling.

4. **Customer account (Agency) creation use case**
- Status: **Adopt**.
- Rationale: Core activation and monetization journey that must be explicit in upstream planning artifacts.

5. **Sanzu administrative outcome and KPI evaluation**
- Status: **Adopt**.
- Rationale: KPI governance is now mandatory to operationalize pilot success criteria, detect risk early, and support data-backed rollout decisions.

6. **In-product inbox for process emails**
- Status: **Adopt with constraints**.
- Rationale: High operational value for transparency and coordination; requires strict RBAC, immutable audit logs, and clear retention/deletion policy before rollout.

7. **Public website for non-authenticated users**
- Status: **Adopt**.
- Rationale: Essential GTM and acquisition layer for agencies evaluating Sanzu before login; required to support trust-building, demand capture, and measurable top-of-funnel performance.

### Decisions
 
- Opportunity phase will prioritize operational reliability and clarity outcomes over feature breadth.
- High-risk automation concepts remain post-MVP until legal/process confidence is proven.
- Commercial optimization is validated through pilot usage and expansion behavior, not assumed upfront.
- Administrative observability for outcomes and KPI performance is a required capability, not optional reporting.
- Process alias convention is fixed as `processslug@sanzy.ai` for process-level communications.
- Inbox-style sent/received email visibility is prioritized with compliance-grade access and audit controls.
- Public non-auth website is a required surface with defined conversion, analytics, and compliance goals.
- vNext opportunity addendum (mission control/admin/copilot):
  - `_bmad-output/opportunity/opportunity-addendum-mission-control-admin-copilot-2026-02-14.md`
- Opportunity run (portfolio shaping across all ideas/themes):
  - `_bmad-output/opportunity/opportunity-run-all-ideas-2026-02-14.md`
 
## Promotion Instructions
- Target canonical folder: `03-Opportunity-Shaping/`
- This status artifact remains in `_bmad-output/opportunity/` until formal promotion is approved.
