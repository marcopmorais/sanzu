# Automation Definitions - Sanzu PDLC 3.5

Owner: Product Ops
Version: 3.5.1
Review cadence: Weekly

## Repository Integration Override
- ClickUp actions are disabled for PDLC workflows in this repository mode.
- Treat ClickUp automation definitions below as reference-only.
- Enforce PDLC checks through local artifacts and local workflow logic only.

## Automation A - Discovery Exit Gate
Trigger: status `Discovery -> Definition`
Rule: block if Strategic_Doc_Link missing OR Evidence_Level < Medium OR Interview_Count < 5
Action on fail: revert to Discovery + comment missing fields

## Automation B - Definition to Design Gate
Trigger: status `Definition -> Design`
Rule: block if Figma_File_Link missing
Action on fail: revert to Definition + comment

## Automation C - Design to Architecture Review Gate
Trigger: status `Design -> Architecture Review`
Rule: block if UX_Maturity != Build-approved OR Figma_Flow_Link missing OR Figma_Frame_IDs missing OR Edge_States_Documented=false OR Accessibility_Checked=false OR UX_Doc_Link missing OR UX revalidation pending
Action on fail: revert to Design + comment

## Automation D - Figma Version Revalidation
Trigger: `Figma_Version` changed
Action:
- set UX_Revalidation_Required=true
- set UX_Revalidated=false
- add comment requiring UX approval

## Automation E - Architecture to Ready for Build Gate
Trigger: status `Architecture Review -> Ready for Build`
Rule: block if architecture/security/compliance/NFR flags not true OR Figma_File_Link missing OR Dev_Handoff_Confirmed=false OR (Architecture_Impact=true and Architecture_Doc_Link missing)
Action on fail: revert to Architecture Review + comment

## Automation F - Ready for Build to Building Gate (DoR + Sensitive Scope)
Trigger: status `Ready for Build -> Building`
Rule: block if any DoR field is missing/false; also block Sanzu sensitive-scope work unless Role_Permission_Matrix_Updated=true, Audit_Trail_Model_Updated=true, Decision_Log_Entry_ID present, and Figma_Role_Variations_Confirmed=true
Action on fail: revert to Ready for Build + comment; set BMBuilder_Ready=false

## Automation G - QA to Ready for Release Gate
Trigger: status `QA -> Ready for Release`
Rule: block if Acceptance_Tests_Pass=false OR NFR_Tests_Pass=false OR Audit_Logging_Validated=false OR Role_Permission_Validated=false OR Release_Metric missing OR Baseline_Value missing OR Metrics_Doc_Link missing
Action on fail: revert to QA + comment

## Automation H - Ready for Release to Released Gate
Trigger: status `Ready for Release -> Released`
Rule: block if Release_Metric missing OR Baseline_Value missing OR Metrics_Doc_Link missing OR Rollback_Plan_Link missing OR Observation_Window_Days <= 0
Action on fail: revert to Ready for Release + comment

## Automation I - Released to Measuring Automation
Trigger: status `Released -> Measuring`
Action:
- auto-create growth experiment task in `08_Growth`

## Automation J - Measuring to Iterating Gate
Trigger: status `Measuring -> Iterating`
Rule: block if Kill_or_Scale_Decision missing OR ROI_Recalculated=false
Action on fail: revert to Measuring + comment

## Automation K - Iterating to Closed Pass
Trigger: status `Iterating -> Closed`
Rule: pass
Action on pass: close lifecycle item

## BMBuilder Trigger Contract
Trigger source: `Ready for Build -> Building`
Rule: BMBuilder_Ready=true and all pre-build validation checks pass (`ops/config/bmbuilder_workflow.yaml`)
Action:
- call BMBuilder webhook
- attach pre-build validation report
