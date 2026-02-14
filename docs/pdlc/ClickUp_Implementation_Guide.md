# Local PDLC Implementation Guide - Sanzu PDLC 3.5

Owner: Product Ops
Version: 3.6.0
Review cadence: Weekly

## Integration Status
- ClickUp integration removed from PDLC workflows.
- Figma integration removed from PDLC workflows.
- PDLC execution is local-artifact only.

## Local Structure
- Operational status: `_bmad-output/status/`
- Story and sprint tracking: `_bmad-output/implementation-artifacts/`
- UX references: `_bmad-output/design/` and `_bmad-output/planning-artifacts/`
- Governance and policy: `docs/`

## Status Model
`Draft -> Discovery -> Definition -> Design -> Architecture Review -> Ready for Build -> Building -> QA -> Ready for Release -> Released -> Measuring -> Iterating -> Closed`

## Mandatory Local Fields
### Strategy and traceability
- Strategic_Doc_Link
- Strategy_Link
- Initiative_ID
- Feature_ID
- Economic_Hypothesis
- Decision_Log_Entry_ID (required when Structural_Impact=true)

### UX governance
- UX_Spec_Link
- UX_Flow_Link
- UX_State_Matrix_Link
- UX_Revision
- UX_Maturity
- Edge_States_Documented
- Accessibility_Checked
- UX_Doc_Link
- Dev_Handoff_Confirmed

### Architecture and compliance
- Architecture_Approved
- Security_Approved
- Data_Compliance_Approved
- NFR_Defined

### Release and measurement
- Release_Metric
- Baseline_Value
- Expected_Impact
- Observation_Window_Days
- Metrics_Doc_Link
- Rollback_Plan_Link

## Enforced Transitions
- Discovery -> Definition: block without strategy link, evidence >= Medium, interview_count >= 5.
- Definition -> Design: block without UX_Spec_Link.
- Design -> Architecture Review: block unless Build-approved UX, local UX references, accessibility, and UX doc are complete.
- Architecture Review -> Ready for Build: block unless architecture/security/compliance approvals and dev handoff are complete.
- Ready for Build -> Building: block unless full DoR + BMBuilder pre-build validation pass.
- QA -> Ready for Release: block unless DoD tests + release metric baseline + metrics doc are complete.
- Ready for Release -> Released: block unless release metric, baseline, metrics doc, rollback plan, and observation window are present.
- Measuring -> Iterating: block unless ROI recalculation and Kill/Iterate/Scale decision are complete.
- Iterating -> Closed: allowed only after Iterating review completion.

## BMBuilder Activation Condition
`Ready for Build -> Building` triggers BMBuilder only when `BMBuilder_Ready=true` and all pre-build rules in `ops/config/bmbuilder_workflow.yaml` pass.

## Governance Notes
- No feature can move past Design without valid local UX references.
- No feature can move to Ready for Build without architecture and compliance approvals.
- No release can proceed without an explicit measurable outcome definition.
