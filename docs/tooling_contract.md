# Tooling Contract

## Integration Mode

- **ClickUp integrations are ENABLED** for PDLC workflows.
- Figma integrations are removed from PDLC workflows.
- Do not call `figma.*` tools for PDLC execution.
- ClickUp MCP tools (`mcp__claude_ai_ClickUp__clickup_*`) are available and SHOULD be used for PDLC state management.

## ClickUp Configuration

- **Workspace ID:** 90152323093
- See `docs/pdlc/ClickUp_Workflow_Guide.md` for the full agent-by-agent workflow.

## ClickUp Workspace Structure

### Space 1: Product & Strategy (ID: 901510198367)

| Folder | ID | Lists |
|--------|----|-------|
| Ideas & Innovation | 901514609029 | Idea Inbox (901521447909), Under Evaluation (901521447911), Feature Requests (901521447913), Parked & Rejected (901521447916) |
| 00_Portfolio | 901514609084 | Opportunity Briefs (901521447988), Feature Prioritization RICE (901521447989) |
| 01_Strategy | 901514609047 | Vision Documents (901521447923), Strategy Addendums (901521447924) |
| 02_Discovery | 901514609055 | Market Research (901521447929), UX Discovery (901521447931) |
| 03_Definition | 901514609059 | PRDs (901521447940), Epics & Stories (901521447942), Requirements (901521447946) |
| 04_Design | 901514609063 | Wireframes & Prototypes (901521447951), Design System (901521447953), Journey Maps (901521447955) |
| 05_Architecture | 901514609075 | Architecture Decisions (901521447975), Readiness Reports (901521447979) |

### Space 2: Engineering (ID: 901510198384)

| Folder | ID | Lists |
|--------|----|-------|
| Core Platform (Epics 1-11) | 901514609054 | Epic 1 (901521447933), Epic 2 (901521447934), Epic 3 (901521447935), Epic 4 (901521447936), Epic 5 (901521447937), Epic 6 (901521447938), Epic 7 (901521447939), Epic 8 (901521447941), Epic 9 (901521447943), Epic 10 (901521447947), Epic 11 (901521447948) |
| Admin Cockpit (Epics 12-21) | 901514609065 | Epic 12 (901521447954), Epic 13 (901521447957), Epic 14 (901521447958), Epic 15 (901521447960), Epic 16 (901521447962), Epic 17 (901521447967), Epic 18 (901521447968), Epic 19 (901521447971), Epic 20 (901521447974), Epic 21 (901521447977) |
| Sprint Management | 901514609085 | Current Sprint (901521447990), Sprint Backlog (901521447991), Sprint Archive (901521447992) |
| Tech Debt & Bugs | 901514609086 | Bug Triage (901521447994), Tech Debt (901521447995) |
| Releases | 901514609088 | Release Tracking (901521447996) |

### Space 3: UX & Design (ID: 901510198393)

| Folder | ID | Lists |
|--------|----|-------|
| UX Research | 901514609062 | User Research (901521447961), Expert Reviews (901521447964) |
| Journey Maps & Flows | 901514609064 | Journey Maps (901521447969), User Flows (901521447970), Service Blueprints (901521447972) |
| Wireframes & Prototypes | 901514609067 | Wireframes (901521447976), HTML Prototypes (901521447980) |
| Design System | 901514609070 | Tokens & Components (901521447983), Design Specs (901521447984) |
| UX Specs | 901514609071 | Per-Story UX Checks (901521447985), Gap Analysis (901521447986) |

### Space 4: Go-To-Market (ID: 901510198395)

| Folder | ID | Lists |
|--------|----|-------|
| GTM & Launch | 901514609089 | Launch Plan (901521447997), Positioning & Narrative (901521447998), Sales Enablement (901521448000) |
| Pricing & Packaging | 901514609090 | Pricing Model (901521448001), Partnerships (901521448002) |
| Metrics & Analytics | 901514609091 | KPI Definitions (901521448004), Event Taxonomy (901521448005), OKRs & Roadmap (901521448006) |
| Sales Pipeline | 901514609092 | Prospects (901521448008) |
| Tenant Onboarding | 901514609093 | Onboarding Checklists (901521448009) |

### Space 5: Governance & Operations (ID: 901510198473)

| Folder | ID | Lists |
|--------|----|-------|
| Compliance & Privacy | 901514609098 | GDPR & Data Retention (901521448020), Phase Gates (901521448021) |
| Risk & Governance | 901514609099 | Risk Register (901521448022), Governance Model (901521448023) |
| Operations | 901514609100 | CI/CD & Deployments (901521448026), Incidents & Postmortems (901521448028) |
| Knowledge Base | 901514609101 | Knowledge OS (901521448031), PDLC Operating Model (901521448033), ADRs (901521448035) |
| Customer Success | 901514609217 | Support Tickets (901521448142), Customer Health (901521448143), Churn Watchlist (901521448144) |
| Finance | 901514609219 | Budget Tracking (901521448147), Expenses (901521448149), Invoices (901521448150) |
| Legal | 901514609223 | Contracts (901521448202), Corporate Governance (901521448203), IP & Registrations (901521448204) |
| Investor Relations | 901514609224 | Fundraising (901521448206), Board Updates (901521448207) |
| Vendors & Tools | 901514609226 | Subscriptions (901521448208), Vendor Contracts (901521448209) |
| Team & Admin | 901514609103 | Meeting Notes (901521448038), Hiring Pipeline (901521448210), Employee Onboarding (901521448211) |

## Active Tooling

- Keep operational status in local artifacts under `_bmad-output/status/` AND sync key state to ClickUp.
- Keep UX design traceability in local artifacts under `_bmad-output/design/` and `_bmad-output/planning-artifacts/`.
- Local artifacts remain the source of truth for detailed content; ClickUp is the source of truth for status, tracking, and discoverability.

## Dual-Write Rule

When creating or updating PDLC artifacts:
1. Write the full artifact to `_bmad-output/<phase>/<artifact_name>.md` (local — detailed content)
2. Create or update the corresponding ClickUp task/doc with a summary and link to the local file
3. Status changes MUST be reflected in ClickUp (task status updates)
