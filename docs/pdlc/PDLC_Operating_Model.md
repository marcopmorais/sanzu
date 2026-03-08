# PDLC Operating Model - Sanzu PDLC 3.6

Owner: Product Ops
Version: 3.6.0
Review cadence: Weekly

## System Contract
- Local artifacts under `_bmad-output/` = operational execution system.
- Local docs under `docs/` = knowledge and policy system.
- Local UX artifacts under `_bmad-output/design/` and `_bmad-output/planning-artifacts/` = experience source of truth.
- BMBuilder = deterministic delivery engine.
- Build is blocked unless all three local systems plus BMBuilder are aligned.

## Repository Integration Override
- ClickUp and Figma integrations are removed from repository PDLC operation.
- PDLC execution must use local artifacts under `_bmad-output/` and `docs/` for operational status and UX traceability.
- Do not execute `clickup.*` or `figma.*` tools from PDLC workflows.

## Phase Model
| Phase | Objective | Mandatory artifacts | Primary owner | Entry criteria | Exit criteria | Risk reduced | Economic validation |
|---|---|---|---|---|---|---|---|
| 0. Portfolio Framing | Align initiative to strategy and capital class | Portfolio brief, theme link, capital type | Portfolio Council | Strategic theme identified | Funding posture approved | Strategic drift | Strategic fit and investment size scored |
| 1. Strategy | Define initiative outcome and constraints | Strategic plan doc, outcome map, hypothesis | PM | Portfolio approval | Strategic_Doc_Link approved | Ambiguous direction | Expected impact defined |
| 2. Discovery | Validate problem and evidence | Discovery notes, interviews, problem statement | PM + Research | Strategy linked | Evidence >= Medium and interviews >= 5 | Building wrong thing | Early value signal confidence set |
| 3. Definition | Convert evidence to executable scope | PRD, epics, acceptance criteria, dependencies | PM + Eng Lead | Discovery gate pass | Acceptance criteria + dependencies complete | Scope ambiguity | Economic hypothesis attached |
| 4. Design | Produce buildable UX with traceability | UX spec/flow/state references, UX doc | UX Lead | Definition gate pass | UX_Maturity = Build-approved | UX execution risk | UX supports measurable behavior |
| 5. Architecture Review | Validate technical/compliance impact | Architecture review, deployment design, security/compliance checks | Architect | Build-approved UX | Architecture/Security/Data approvals true, Deployment_Design_Approved = true | Technical and compliance risk | NFR and implementation cost checked |
| 5.5. CI/CD Scaffold | Build and validate the delivery pipeline before any story starts | CI pipeline config, quality gate spec, test framework init, pipeline green baseline | TEA + Eng Lead | DoR gate pass | CI_Pipeline_Green = true, Quality_Gates_Defined = true | Pipeline absent at build start | Cost-to-deliver tracked with automation coverage |
| 6. Delivery (BMBuilder) | Deterministic build execution | Build plan, test pack, risk summary | Eng Lead | DoR pass AND CI_Pipeline_Green = true | Build + QA complete | Build drift | Cost-to-deliver tracked |
| 7. Release | Controlled launch with metric baseline | Release plan, rollback plan, baseline metric | Release Manager | QA gate pass | Released with monitoring | Release failure risk | Baseline vs expected impact traceable |
| 8. Growth & Experimentation | Measure and decide | Experiment report, observation data | Growth Lead | Released | Decision = Scale/Iterate/Kill | False-positive impact | Impact measurement completed |
| 9. Post-Release Learning | Recalculate initiative economics | ROI recalculation and decision log | Finance + PM | Observation window complete | ROI_Recalculated=true | Economic blind spots | Updated ROI and payback |
| 10. Lifecycle Management | Continue, iterate, or sunset | Lifecycle closure record | Product Ops | ROI decision available | Closed | Zombie initiatives | Capital reallocation decision logged |

## Status Model
`Draft -> Discovery -> Definition -> Design -> Architecture Review -> CI/CD Scaffold -> Ready for Build -> Building -> QA -> Ready for Release -> Released -> Measuring -> Iterating -> Closed`

## Definition of Ready (Ready for Build)
- Strategic link exists
- Economic hypothesis defined
- UX_Maturity = Build-approved
- Edge_States_Documented = true
- Accessibility_Checked = true
- Architecture_Approved = true
- Deployment_Design_Approved = true
- Security_Approved = true
- Data_Compliance_Approved = true
- Acceptance_Criteria_Defined = true
- NFR_Defined = true
- Dependencies_Mapped = true
- CI_Pipeline_Green = true
- Quality_Gates_Defined = true

Any missing item = hard block. CI_Pipeline_Green = true requires a successful pipeline run on a green baseline — not just a config file existing.

## Definition of Done (Ready for Release)
- Acceptance tests pass
- NFR tests pass
- Audit logging validated
- Role-permission matrix validated
- Baseline metric captured
- Rollback plan documented
- Rollback path tested on staging (not just documented)
- Staging smoke tests pass via CI pipeline

## Sanzu-Specific Enforcement
- Audit trail updates are mandatory when changing document upload, role assignment, or process state transitions.
- Structural-impact changes require Decision_Log entry.
- Sensitive-scope features require role-variation coverage in local UX artifacts before build.
- No story may begin implementation until CI_Pipeline_Green = true is confirmed in `_bmad-output/5.5-cicd/pipeline-baseline.md`. This is a hard block enforced by the SM agent at sprint planning.
- The deployment design (`5-architecture/deployment-design.md`) must be produced by the Architect and approved before TEA begins CI/CD scaffold.
- Rollback path must be executed (not just written) on staging before production promotion is approved.
