# Risk Taxonomy - Sanzu PDLC 3.5

Owner: Governance Lead
Version: 3.5.1
Review cadence: Monthly

## Risk Classes
| Risk Type | Definition | Primary trigger | Primary owner |
|---|---|---|---|
| Strategic Risk | Initiative no longer supports portfolio strategy | Theme mismatch or invalid strategy link | Portfolio Council |
| Product Risk | Problem/solution mismatch or weak user evidence | Discovery evidence below threshold | PM |
| Technical Risk | Architecture, performance, reliability exposure | NFR undefined or architecture rejection | Architect |
| Execution Risk | Delivery instability, dependency or scope failure | DoR failures, dependency conflicts | Engineering Lead |
| Financial Risk | ROI underperformance or overspend | Cost growth, low impact realization | Finance |
| Compliance Risk | Security/privacy/audit control failure | Missing approvals, audit model gaps | Compliance Lead |

## Blocking Thresholds
- Strategic: block at Discovery if no Strategic_Doc_Link.
- Product: block at Discovery/Definition if evidence below gate.
- Technical: block at Architecture Review if approvals missing.
- Execution: block at Ready for Build if DoR incomplete.
- Financial: block release close if ROI not recalculated.
- Compliance: block build when sensitive-scope controls are incomplete.

## Phase Mitigation Checklist
| Phase | Mandatory mitigation |
|---|---|
| Discovery | Problem statement, evidence level, interview count |
| Definition | Acceptance criteria, dependency mapping, economic hypothesis |
| Design | Build-approved UX, accessibility check, role variants |
| Architecture | Security + data compliance approvals, NFR validation |
| Delivery | BMBuilder pre-build hard stop, dependency checks |
| Release | Rollback plan + baseline metric + observation window |
| Measuring | ROI recalculation + kill/iterate/scale decision |

## Escalation Logic
- High compliance risk: immediate escalation to Compliance Lead + Architect.
- High financial risk: immediate escalation to Finance + Portfolio Council.
- Repeated gate failures (>2 cycles): mandatory governance review in weekly loop.
