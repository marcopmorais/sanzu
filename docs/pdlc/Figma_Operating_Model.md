# UX Operating Model - Sanzu PDLC 3.5

Owner: UX Lead
Version: 3.6.0
Review cadence: Weekly

## Artifact Workspace
`_bmad-output/design/` and `_bmad-output/planning-artifacts/`

## File Naming Convention
`[Sanzu]-[Feature]-[Phase]-vX.X`

## Mandatory UX Sections
- 00_Vision
- 01_Research
- 02_User_Flows
- 03_Wireframes
- 04_HiFi
- 05_Design_System
- 06_Dev_Handoff
- 07_Experiments

## UX Maturity Levels
- Concept: initial interaction direction only.
- Structured: end-to-end happy path drafted.
- Test-ready: key states and variants complete for validation.
- Build-approved: complete flow + edge/error/loading states + role variants + handoff package.

## Build-Approved Checklist
- Full user flow coverage.
- Edge states documented.
- Error states documented.
- Loading states documented.
- Role-based variations documented.
- Components mapped to design system.
- WCAG AA baseline validated on key flows.
- Dev handoff artifact includes:
  - Interaction logic
  - State matrix
  - API assumptions
  - Token references

## Local Traceability Contract
Each feature must include:
- UX_Spec_Link
- UX_Flow_Link
- UX_State_Matrix_Link
- UX_Revision

## UX Revision Change Control
When `UX_Revision` changes:
- Set `UX_Revalidation_Required=true`
- Set `UX_Revalidated=false`
- Block forward status transitions until UX reapproval

## Sanzu-Specific Requirement
For features affecting document upload, role permissions, or process state transitions:
- UX artifacts must explicitly show role-based variation states.
- `UX_Role_Variations_Confirmed=true` is required before build.
