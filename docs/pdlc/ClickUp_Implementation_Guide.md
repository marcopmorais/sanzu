# ClickUp Implementation Guide - Sanzu PDLC 3.5

Owner: Product Ops
Version: 3.5.1
Review cadence: Weekly

## Repository Integration Override
- ClickUp actions are disabled for PDLC workflows in this repository mode.
- This guide is retained for future re-enable/reference only.
- Do not execute ClickUp write/read actions as part of current PDLC runs.

## Space
`Sanzu Product System`

## Folder Structure
- 00_Portfolio
- 01_Strategy
- 02_Discovery
- 03_Definition
- 04_Design
- 05_Architecture
- 06_Delivery
- 07_Release
- 08_Growth
- 09_Lifecycle
- 10_Knowledge_Base

## Official Template Mapping
| Folder | Template |
|---|---|
| 01_Strategy | Product Managers Strategic Plan Template |
| 02_Discovery | Quick Start Product Management Template |
| 03_Definition | Product Development Work Breakdown Structure Template |
| 06_Delivery | Software Development Template |
| 07_Release | New Product Development / Example Project Plan Template |
| 08_Growth | Product Roadmap Template |
| 09_Lifecycle | Product Management Quarterly Review Template |
| 10_Knowledge_Base | Sanzu Knowledge OS Docs |

## Status Model
`Draft -> Discovery -> Definition -> Design -> Architecture Review -> Ready for Build -> Building -> QA -> Ready for Release -> Released -> Measuring -> Iterating -> Closed`

## Mandatory Fields
### Strategy and traceability
- Strategic_Doc_Link
- Strategy_Link
- Initiative_ID
- Feature_ID
- Economic_Hypothesis
- Decision_Log_Entry_ID (required when Structural_Impact=true)

### Figma governance
- Figma_File_Link
- Figma_Flow_Link
- Figma_Frame_IDs
- Figma_Version
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
- Definition -> Design: block without Figma_File_Link.
- Design -> Architecture Review: block unless Build-approved UX, frame references, accessibility, and UX doc are complete.
- Architecture Review -> Ready for Build: block unless architecture/security/compliance approvals and dev handoff are complete.
- Ready for Build -> Building: block unless full DoR + BMBuilder pre-build validation pass.
- QA -> Ready for Release: block unless DoD tests + release metric baseline + metrics doc are complete.
- Ready for Release -> Released: block unless release metric, baseline, metrics doc, rollback plan, and observation window are present.
- Measuring -> Iterating: block unless ROI recalculation and Kill/Iterate/Scale decision are complete.
- Iterating -> Closed: allowed only after Iterating review completion.

## BMBuilder Activation Condition
`Ready for Build -> Building` triggers BMBuilder only when `BMBuilder_Ready=true` and all pre-build rules in `ops/config/bmbuilder_workflow.yaml` pass.

## Governance Notes
- No feature can move past Design without valid Figma links.
- No feature can move to Ready for Build without architecture and compliance approvals.
- No release can proceed without an explicit measurable outcome definition.
