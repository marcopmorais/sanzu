# PDLC Operating Model - Sanzu PDLC 3.5

Owner: Product Ops
Version: 3.5.1
Review cadence: Weekly

## System Contract
- ClickUp = operational execution system.
- ClickUp Docs = knowledge and policy system.
- Figma = experience source of truth.
- BMBuilder = deterministic delivery engine.
- Build is blocked unless all four systems are aligned.

## Repository Integration Override
- For this repository operating mode, ClickUp actions are disabled.
- PDLC execution must use local artifacts under `_bmad-output/` for operational status.
- Do not execute `clickup.*` tools from PDLC workflows.

## Phase Model
| Phase | Objective | Mandatory artifacts | Primary owner | Entry criteria | Exit criteria | Risk reduced | Economic validation |
|---|---|---|---|---|---|---|---|
| 0. Portfolio Framing | Align initiative to strategy and capital class | Portfolio brief, theme link, capital type | Portfolio Council | Strategic theme identified | Funding posture approved | Strategic drift | Strategic fit and investment size scored |
| 1. Strategy | Define initiative outcome and constraints | Strategic plan doc, outcome map, hypothesis | PM | Portfolio approval | Strategic_Doc_Link approved | Ambiguous direction | Expected impact defined |
| 2. Discovery | Validate problem and evidence | Discovery notes, interviews, problem statement | PM + Research | Strategy linked | Evidence >= Medium and interviews >= 5 | Building wrong thing | Early value signal confidence set |
| 3. Definition | Convert evidence to executable scope | PRD, epics, acceptance criteria, dependencies | PM + Eng Lead | Discovery gate pass | Acceptance criteria + dependencies complete | Scope ambiguity | Economic hypothesis attached |
| 4. Design | Produce buildable UX with traceability | Figma file/flow/frame IDs, UX doc | UX Lead | Definition gate pass | UX_Maturity = Build-approved | UX execution risk | UX supports measurable behavior |
| 5. Architecture Review | Validate technical/compliance impact | Architecture review, security/compliance checks | Architect | Build-approved UX | Architecture/Security/Data approvals true | Technical and compliance risk | NFR and implementation cost checked |
| 6. Delivery (BMBuilder) | Deterministic build execution | Build plan, test pack, risk summary | Eng Lead | DoR gate pass | Build + QA complete | Build drift | Cost-to-deliver tracked |
| 7. Release | Controlled launch with metric baseline | Release plan, rollback plan, baseline metric | Release Manager | QA gate pass | Released with monitoring | Release failure risk | Baseline vs expected impact traceable |
| 8. Growth & Experimentation | Measure and decide | Experiment report, observation data | Growth Lead | Released | Decision = Scale/Iterate/Kill | False-positive impact | Impact measurement completed |
| 9. Post-Release Learning | Recalculate initiative economics | ROI recalculation and decision log | Finance + PM | Observation window complete | ROI_Recalculated=true | Economic blind spots | Updated ROI and payback |
| 10. Lifecycle Management | Continue, iterate, or sunset | Lifecycle closure record | Product Ops | ROI decision available | Closed | Zombie initiatives | Capital reallocation decision logged |

## Status Model
`Draft -> Discovery -> Definition -> Design -> Architecture Review -> Ready for Build -> Building -> QA -> Ready for Release -> Released -> Measuring -> Iterating -> Closed`

## Definition of Ready (Ready for Build)
- Strategic link exists
- Economic hypothesis defined
- UX_Maturity = Build-approved
- Edge_States_Documented = true
- Accessibility_Checked = true
- Architecture_Approved = true
- Security_Approved = true
- Data_Compliance_Approved = true
- Acceptance_Criteria_Defined = true
- NFR_Defined = true
- Dependencies_Mapped = true

Any missing item = hard block.

## Definition of Done (Ready for Release)
- Acceptance tests pass
- NFR tests pass
- Audit logging validated
- Role-permission matrix validated
- Baseline metric captured
- Rollback plan documented

## Sanzu-Specific Enforcement
- Audit trail updates are mandatory when changing document upload, role assignment, or process state transitions.
- Structural-impact changes require Decision_Log entry.
- Sensitive-scope features require role-variation coverage in Figma before build.
