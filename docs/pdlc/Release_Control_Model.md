# Release Control Model - Sanzu PDLC 3.5

Owner: Release Manager
Version: 3.5.1
Review cadence: Weekly

## Repository Integration Override
- ClickUp and Figma integrations are removed for PDLC workflows in this repository mode.
- Release gates are evaluated from local artifacts only.

## QA -> Ready for Release Gate
Transition is blocked unless all conditions pass:
- Acceptance_Tests_Pass=true
- NFR_Tests_Pass=true
- Audit_Logging_Validated=true
- Role_Permission_Validated=true
- Release_Metric not empty
- Baseline_Value populated
- Metrics_Doc_Link populated

## Ready for Release -> Released Gate
Transition is blocked unless all conditions pass:
- Release_Metric not empty
- Baseline_Value populated
- Metrics_Doc_Link populated
- Rollback_Plan_Link populated
- Observation_Window_Days > 0

## Mandatory Release Artifacts
- Release plan
- Rollback plan
- Baseline metric snapshot
- Metrics document link
- Monitoring runbook

## Post-Release Automation
On `Released`:
- Auto-transition to `Measuring`
- Create Growth experiment task in folder `08_Growth`
- Start observation window countdown

## Close Condition
`Measuring` cannot close unless:
- Kill_or_Scale_Decision selected
- ROI_Recalculated=true
