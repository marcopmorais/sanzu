# Phase Gates and Checklists - Sanzu PDLC 3.5

Owner: PMO
Version: 3.5.1
Review cadence: Weekly

## Transition Gate Table
| Transition | Block Conditions |
|---|---|
| Discovery -> Definition | Strategic_Doc_Link missing OR Evidence_Level < Medium OR Interview_Count < 5 |
| Definition -> Design | UX_Spec_Link missing |
| Design -> Architecture Review | UX_Maturity != Build-approved OR UX_Flow_Link missing OR UX_State_Matrix_Link missing OR Edge_States_Documented=false OR Accessibility_Checked=false OR UX_Doc_Link missing |
| Architecture Review -> Ready for Build | Architecture_Approved=false OR Security_Approved=false OR Data_Compliance_Approved=false OR NFR_Defined=false OR UX_Spec_Link missing OR Dev_Handoff_Confirmed=false |
| Ready for Build -> Building | Any DoR field false; UX revalidation pending; Decision_Log missing for structural impact; sensitive-scope controls unmet |
| QA -> Ready for Release | Release_Metric missing OR Baseline_Value missing OR Metrics_Doc_Link missing OR DoD test flags false |
| Ready for Release -> Released | Release_Metric missing OR Baseline_Value missing OR Metrics_Doc_Link missing OR Rollback_Plan_Link missing OR Observation_Window_Days <= 0 |
| Measuring -> Iterating | ROI_Recalculated=false OR Kill_or_Scale_Decision missing |
| Iterating -> Closed | Iteration review incomplete |

## Design Gate Checklist
- [ ] UX_Spec_Link exists
- [ ] UX_Flow_Link exists
- [ ] UX_State_Matrix_Link populated
- [ ] UX_Revision recorded
- [ ] UX_Maturity = Build-approved
- [ ] Edge states documented
- [ ] Accessibility checked
- [ ] UX_Doc_Link exists

## Architecture Entry Checklist
- [ ] Build-approved UX verified
- [ ] Dev handoff confirmed in local handoff artifact
- [ ] UX revalidation not pending

## Ready for Build Checklist
- [ ] Architecture_Approved
- [ ] Security_Approved
- [ ] Data_Compliance_Approved
- [ ] NFR_Defined
- [ ] UX references still valid
- [ ] Architecture_Doc_Link exists if Architecture_Impact=true

## Ready for Release Checklist
- [ ] Acceptance tests pass
- [ ] NFR tests pass
- [ ] Audit logging validated
- [ ] Role-permission validation complete
- [ ] Release_Metric set
- [ ] Baseline_Value set
- [ ] Metrics_Doc_Link set
- [ ] Rollback_Plan_Link set
- [ ] Observation_Window_Days > 0

## Sanzu Sensitive Scope Checklist
Apply if document upload, role permissions, or process state transitions are impacted:
- [ ] Role_Permission_Matrix_Updated=true
- [ ] Audit_Trail_Model_Updated=true
- [ ] Decision_Log_Entry_ID present
- [ ] UX_Role_Variations_Confirmed=true
