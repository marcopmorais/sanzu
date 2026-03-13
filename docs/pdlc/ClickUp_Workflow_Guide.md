# BMAD Agent ClickUp Workflow Guide

> How each BMAD agent uses ClickUp in the Sanzu PDLC workflow.

## Core Principle: Dual-Write

Every PDLC artifact follows the dual-write rule:

1. **Local file** (`_bmad-output/`) — full detailed content (source of truth for content)
2. **ClickUp** — status tracking, summaries, discoverability (source of truth for state)

Local artifacts are rich and detailed. ClickUp tasks/docs are lightweight pointers with status.

## ClickUp MCP Tools Available

All agents can use these tools (prefix: `mcp__claude_ai_ClickUp__clickup_`):

| Tool | Use for |
|------|---------|
| `clickup_create_task` | Create idea tasks, story tasks, bug tasks |
| `clickup_update_task` | Update task status, description, assignees |
| `clickup_get_task` | Read task details |
| `clickup_search` | Find tasks, docs, or any content |
| `clickup_create_document` | Create a new ClickUp Doc |
| `clickup_create_document_page` | Add a page to an existing doc |
| `clickup_update_document_page` | Update page content |
| `clickup_create_task_comment` | Add comments to tasks |
| `clickup_get_task_comments` | Read task comments |

## Workspace IDs Quick Reference

```yaml
workspace_id: "90152323093"

# Space IDs
product_strategy: "901510198367"
engineering: "901510198384"
ux_design: "901510198393"
go_to_market: "901510198395"
governance_ops: "901510198473"

# Space 1: Product & Strategy — Folder IDs
ideas_innovation: "901514609029"
portfolio: "901514609084"
strategy: "901514609047"
discovery: "901514609055"
definition: "901514609059"
design: "901514609063"
architecture: "901514609075"

# Space 2: Engineering — Folder IDs
core_epics_folder: "901514609054"
admin_epics_folder: "901514609065"
sprint_management: "901514609085"
tech_debt_bugs: "901514609086"
releases: "901514609088"

# Space 3: UX & Design — Folder IDs
ux_research: "901514609062"
journey_maps: "901514609064"
wireframes: "901514609067"
design_system: "901514609070"
ux_specs: "901514609071"

# Space 4: Go-To-Market — Folder IDs
gtm_launch: "901514609089"
pricing: "901514609090"
metrics_analytics: "901514609091"
sales_pipeline: "901514609092"
tenant_onboarding: "901514609093"

# Space 5: Governance & Operations — Folder IDs
compliance: "901514609098"
risk_governance: "901514609099"
operations: "901514609100"
knowledge_base: "901514609101"
customer_success: "901514609217"
finance: "901514609219"
legal: "901514609223"
investor_relations: "901514609224"
vendors_tools: "901514609226"
team_admin: "901514609103"

# Company Management — Key List IDs
support_tickets: "901521448142"
customer_health: "901521448143"
churn_watchlist: "901521448144"
budget_tracking: "901521448147"
expenses: "901521448149"
invoices: "901521448150"
contracts: "901521448202"
corporate_governance: "901521448203"
ip_registrations: "901521448204"
fundraising: "901521448206"
board_updates: "901521448207"
subscriptions: "901521448208"
vendor_contracts: "901521448209"
hiring_pipeline: "901521448210"
employee_onboarding: "901521448211"

# Key List IDs (most used by agents)
idea_inbox: "901521447909"
under_evaluation: "901521447911"
feature_requests: "901521447913"
parked_rejected: "901521447916"
prds: "901521447940"
epics_stories: "901521447942"
current_sprint: "901521447990"
sprint_backlog: "901521447991"
bug_triage: "901521447994"
release_tracking: "901521447996"

# Engineering — Epic List IDs
epic_1: "901521447933"
epic_2: "901521447934"
epic_3: "901521447935"
epic_4: "901521447936"
epic_5: "901521447937"
epic_6: "901521447938"
epic_7: "901521447939"
epic_8: "901521447941"
epic_9: "901521447943"
epic_10: "901521447947"
epic_11: "901521447948"
epic_12: "901521447954"
epic_13: "901521447957"
epic_14: "901521447958"
epic_15: "901521447960"
epic_16: "901521447962"
epic_17: "901521447967"
epic_18: "901521447968"
epic_19: "901521447971"
epic_20: "901521447974"
epic_21: "901521447977"
```

---

## Agent Workflows

### Mary (Business Analyst)

**When to use ClickUp:**

1. **New idea captured** (brainstorm, research, customer feedback):
   - Create task in **Idea Inbox** (list `901521447909`)
   - Use this template for the task description:
     ```
     **Source:** [Customer / Internal / Market / Support / Competitor]
     **Problem:** [What pain does this solve?]
     **Who benefits:** [Persona / segment]
     **Rough size:** [XS / S / M / L / XL]
     **Evidence:** [Links, quotes, data]
     **PDLC Phase:** Phase 0 — Idea
     ```
   - Set status to `open` (new idea)

2. **Research completed:**
   - Write full research to `_bmad-output/discovery/` or `_bmad-output/planning-artifacts/research/`
   - Update the idea task in ClickUp with a comment linking to the local file
   - If research validates the idea, move task status forward

3. **Product brief created:**
   - Write to `_bmad-output/planning-artifacts/product-brief-*.md`
   - Create or update a ClickUp Doc page in the **Product Briefs** doc (ID: `2kyqyj0n-1455`) with a summary

**Menu items that trigger ClickUp actions:**
- `[BB] Brainstorm` → After session, capture promising ideas as tasks in Idea Inbox
- `[RR] Research` → After research, update related idea tasks with findings
- `[PB] Product Brief` → After creating brief, update ClickUp doc

---

### John (Product Manager)

**When to use ClickUp:**

1. **Idea evaluation (RICE scoring):**
   - Search Idea Inbox for unscored ideas: `clickup_search` with `keywords: "Phase 0"` filtered to list `901521447909`
   - After scoring, move task to **Under Evaluation** (list `901521447911`)
   - Add RICE scores in a comment: `Impact: X, Confidence: X, Reach: X, Effort: X → Score: X`
   - Update status: `Scored` or `Promoted` or move to Parked/Rejected list

2. **PRD creation:**
   - Write PRD to `_bmad-output/planning-artifacts/prd*.md`
   - Create task in **PRDs** list (ID: `901521447940`) with summary and link to local file
   - Update corresponding ClickUp Doc pages (Core PRD: `2kyqyj0n-1275`, Admin PRD: `2kyqyj0n-1295`)

3. **Epic & story definition:**
   - Write epics to `_bmad-output/definition/epics*.md`
   - Create task in **Epics & Stories** list (ID: `901521447942`) per epic with story count and status
   - Update ClickUp Doc (ID: `2kyqyj0n-1335`)

4. **Implementation readiness check:**
   - Write report to `_bmad-output/planning-artifacts/implementation-readiness-report-*.md`
   - Add comment to the relevant epic task in ClickUp with readiness verdict

5. **Feature request triage:**
   - Review **Feature Requests** list (ID: `901521447913`)
   - Promote valid requests to Idea Inbox for formal evaluation

**Menu items that trigger ClickUp actions:**
- `[PP] Create PRD` → Create PRD task + update doc
- `[EE] Create Epics + Stories` → Create epic tasks in Epics & Stories list
- `[II] Implementation Readiness` → Comment on epic tasks with readiness status
- `[CC] Correct Course` → Update affected task statuses

---

### Bob (Scrum Master)

**When to use ClickUp:**

1. **Sprint planning:**
   - Write sprint plan to `_bmad-output/implementation-artifacts/sprint-plan-*.md`
   - Update sprint-status.yaml locally
   - Create sprint plan task in **Sprint Plans** list (ID: `901521447990`)
   - For each story in the sprint, ensure a task exists in **Core Epics 1-11** (ID: `901521447933`) or **Admin Epics 12-21** (ID: `901521447954`)

2. **Story creation:**
   - Write story spec to `_bmad-output/implementation-artifacts/{epic}-{story}-{slug}.md`
   - Create task in the appropriate epic list with:
     ```
     Name: Story {X.Y} — {Title}
     Description: Summary + link to local spec file
     Status: backlog / in progress / done
     ```

3. **Sprint status updates:**
   - After each story completion, update the story task status in ClickUp to `complete`
   - Add a comment with the commit hash: `Shipped in commit {hash}`
   - Update sprint plan task with progress

4. **Retrospective:**
   - Write retro to `_bmad-output/implementation-artifacts/retrospective-*.md`
   - Add comment to sprint plan task summarizing outcomes

**Menu items that trigger ClickUp actions:**
- `[SP] Sprint Planning` → Create sprint + story tasks
- `[CS] Create Story` → Create story task in epic list
- `[RE] Retrospective` → Comment on sprint task

---

### Amelia (Developer)

**When to use ClickUp:**

1. **Starting a story:**
   - Read story task from ClickUp to get latest context
   - Update task status to `in progress`

2. **Story completed:**
   - Update task status to `complete`
   - Add comment with: commit hash, test results summary, any notes for QA

3. **Bug found during development:**
   - Create task in the appropriate list with `Bug:` prefix in the name
   - Include: repro steps, expected vs actual, affected story/epic

4. **Code review findings:**
   - Add findings as comments on the story task

**Menu items that trigger ClickUp actions:**
- `[DS] Dev Story` → Read story task, update status on start/completion
- `[CR] Code Review` → Comment findings on story task

---

### Winston (Architect)

**When to use ClickUp:**

1. **Architecture document created/updated:**
   - Write to `_bmad-output/planning-artifacts/architecture*.md`
   - Update ClickUp Doc pages in **Architecture & ADRs** (ID: `2kyqyj0n-1315`)

2. **ADR created:**
   - Write ADR to local file
   - Add page to Architecture & ADRs doc with: title, context, decision, consequences

3. **Implementation readiness:**
   - Comment on epic tasks with architecture readiness assessment

**Menu items that trigger ClickUp actions:**
- `[AA] Create Architecture` → Update architecture doc in ClickUp
- `[II] Implementation Readiness` → Comment on epic tasks

---

### Sally (UX Designer)

**When to use ClickUp:**

1. **UX spec created/updated:**
   - Write to `_bmad-output/design/ux-spec.md`
   - Update ClickUp Doc pages in **Design System & UX Spec** (ID: `2kyqyj0n-1435`)

2. **Wireframe/prototype created:**
   - Write Excalidraw/HTML to `_bmad-output/design/wireframes/`
   - Add comment to the relevant story task linking to the wireframe file

3. **UX review completed:**
   - Write review to `_bmad-output/design/frontend-per-story-ux-checks*.md`
   - Comment on story tasks with UX approval/issues

**Menu items that trigger ClickUp actions:**
- `[UX] UX Design` → Update UX spec doc
- `[EW] Excalidraw Wireframe` → Comment on story tasks with wireframe links

---

### Murat (Test Architect)

**When to use ClickUp:**

1. **Test framework setup:**
   - Document in local files
   - Comment on architecture tasks with test strategy decisions

2. **Test coverage reports:**
   - Add comments on epic/story tasks with coverage metrics

3. **NFR assessment:**
   - Comment on architecture tasks with NFR findings

---

### Paige (Technical Writer)

**When to use ClickUp:**

1. **Documentation created:**
   - Write to `_bmad-output/documentation/` or `docs/`
   - Create/update pages in the relevant ClickUp Doc

2. **Knowledge base updated:**
   - Update pages in **Knowledge OS** doc (ID: `2kyqyj0n-1415`)

---

### Barry (Quick Flow Solo Dev)

**When to use ClickUp:**

1. **Before starting work:**
   - Check sprint plan task and story tasks for latest context
   - Update story task status to `in progress`

2. **After completing work:**
   - Update story task status to `complete`
   - Add comment with commit hash and test summary

---

## The PDLC Flow Through ClickUp

```
Phase 0: IDEA
  └─ analyst/pm captures idea → task in Idea Inbox
  └─ pm scores idea (RICE) → task moves to Under Evaluation
  └─ pm promotes/parks/rejects → task moves accordingly

Phase 1: STRATEGY
  └─ analyst creates product brief → ClickUp Doc updated
  └─ pm validates strategy alignment → comment on idea task

Phase 2: DISCOVERY
  └─ analyst runs research → findings linked to idea task
  └─ ux-designer runs UX discovery → ClickUp Doc updated

Phase 3: DEFINITION
  └─ pm creates PRD → task in PRDs list + ClickUp Doc
  └─ pm creates epics → tasks in Epics & Stories list
  └─ architect creates architecture → ClickUp Doc updated

Phase 4: DESIGN
  └─ ux-designer creates wireframes → linked to story tasks
  └─ architect reviews readiness → comments on epic tasks

Phase 5: ARCHITECTURE
  └─ architect creates ADRs → ClickUp Doc updated
  └─ pm/architect run implementation readiness → comments on tasks

Phase 6: DELIVERY
  └─ sm creates sprint plan → task in Sprint Plans list
  └─ sm creates stories → tasks in Epic lists
  └─ dev executes stories → task status updates
  └─ tea validates tests → comments on story tasks

Phase 7: RELEASE
  └─ sm tracks releases → tasks in Release Tracking list

Phase 8: GROWTH
  └─ pm tracks metrics → tasks in Metrics & KPIs list
  └─ analyst tracks sales → tasks in Sales Pipeline list

Phase 9: LIFECYCLE
  └─ pm tracks compliance → tasks in Compliance list
  └─ ops tracks incidents → tasks in Operations list

Phase 10: KNOWLEDGE
  └─ tech-writer maintains docs → ClickUp Docs updated
```

## ClickUp Doc Index

| Doc Name | ID | Contents |
|----------|----|----------|
| Product Vision & Strategy | 2kyqyj0n-1215 | Exec Summary, Vision, Grand Vision, Versioning, Assumption Map |
| Product Briefs | 2kyqyj0n-1455 | Sanzu Brief, Admin Brief, Strategy Addendum |
| Market & UX Research | 2kyqyj0n-1235 | Portugal Market, Grief-Aware UX, SaaS Ops, User Research Plan |
| Discovery Findings | 2kyqyj0n-1475 | Findings Memo, UX Expert Review, Scorecard, Scenario Pack |
| Opportunity Briefs & RICE | 2kyqyj0n-1255 | Core Brief, Mission Control, All Ideas, RICE Matrix |
| PRD — Core Platform | 2kyqyj0n-1275 | Full PRD, Functional Reqs, Non-Functional Reqs, Journeys |
| PRD — Admin Cockpit | 2kyqyj0n-1295 | Admin PRD, Validation Report |
| Architecture & ADRs | 2kyqyj0n-1315 | Core Arch, Admin Arch, ADRs, .NET Azure Spec |
| Epics & Stories | 2kyqyj0n-1335 | Core 1-11, Admin 12-21, RBAC Matrix, V1 Backlog |
| GTM Playbook | 2kyqyj0n-1355 | Launch Plan, Positioning, Comms, Sales Enablement, Pricing |
| Metrics Playbook | 2kyqyj0n-1375 | Metrics Plan, Tracking Spec, KPIs, OKRs |
| Compliance & Governance | 2kyqyj0n-1395 | GDPR/Privacy, Risk Taxonomy, Phase Gates |
| Knowledge OS | 2kyqyj0n-1415 | Architecture System, Decision Log, Operating Model, PDLC |
| Design System & UX Spec | 2kyqyj0n-1435 | UX Spec, Design Specification, Brand System, Tone System |

---

## ClickUp Goals as OKRs (Free Tier: up to 100 goals)

Goals are set up manually in the ClickUp UI: **Sidebar → Goals → + New Goal**.

ClickUp Goals map directly to the OKR framework:
- **Goal Folder** = Objective (the qualitative outcome you're chasing)
- **Goal** = Key Result (the measurable evidence that proves the objective is met)
- **Target** = the number, percentage, currency, or task completion that defines success

> North Star Metric (from `_bmad-output/metrics/metrics_plan.md`):
> **On-time Critical Milestone Completion Rate** — percentage of active cases where critical early obligations are completed on or before deadline without severe rework.

---

### OKR 1: Deliver a Reliable, Complete Platform (Goal Folder)

_Objective: Ship a platform that agencies and families can trust for real case execution._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Core Platform epics shipped | Task | 11/11 epics complete | Epic lists 1-11 task status |
| Admin Cockpit v1 shipped | Task | Epics 12-14 complete | Epic lists 12-14 task status |
| Test coverage ≥ 80% | Percentage | 80% | CI test reports |
| Zero P1/P2 open bugs | Number | 0 | Bug Triage list count |
| Sprint velocity stable | Number | ≥ X stories/sprint (3-sprint avg) | Current Sprint list completion |
| Deployment success rate | Percentage | 100% zero-rollback deploys | Release Tracking list |

---

### OKR 2: Prove User Value in Pilot (Goal Folder)

_Objective: Families and agencies experience measurably better coordination and confidence._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Time-to-first-completed-task ≤ 24h | Number | ≤ 24 hours median | KPI dashboard (`/api/v1/admin/kpi/dashboard`) |
| Case setup time ≤ 2 minutes | Number | ≤ 120 seconds median | KPI dashboard |
| High-priority steps completed without rework ≥ 85% | Percentage | 85% | KPI dashboard |
| Family weekly active participation ≥ 70% | Percentage | 70% | Analytics events (`step_status_changed`) |
| Families understand next action within 24h | Percentage | ≥ 80% (usability test) | UX Research list findings |
| Reduced coordination overhead (agency self-report) | True/False | Confirmed in pilot debrief | Customer Health list |

---

### OKR 3: Achieve Sustainable Agency Adoption (Goal Folder)

_Objective: Agencies activate, stay, and expand — proving product-market fit._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Agency activation rate (14-day) ≥ 80% | Percentage | 80% | KPI dashboard |
| Pilot agency quarterly retention ≥ 85% | Percentage | 85% | Customer Health list |
| 3-5 pilot agencies with live cases | Number | 3 minimum | Prospects list (won stage) |
| Trial-to-paid conversion (baseline) | Percentage | Establish baseline | Invoices list + KPI |
| Invoice collection success rate | Percentage | ≥ 95% | Invoices list |
| Net Promoter Score baseline captured | True/False | NPS survey deployed | Analytics events (`nps_submitted`) |

---

### OKR 4: Launch to Market with Credibility (Goal Folder)

_Objective: Position Sanzu for pilot launch with clear narrative, pricing, and readiness._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Pilot launch checklist complete | Task | All items done | Launch Plan list |
| Positioning narrative validated | True/False | Agency feedback positive | Positioning list |
| Pricing model v1 finalized | True/False | Published to agencies | Pricing Model list |
| Sales enablement kit ready | Task | Deck + demo + FAQ done | Sales Enablement list |
| Tenant onboarding < 15 min | Number | ≤ 15 minutes | Onboarding Checklists list |

---

### OKR 5: Operate with Trust and Compliance (Goal Folder)

_Objective: Run a platform that is safe, auditable, and compliant from day one._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| GDPR privacy review passed | True/False | Passed | GDPR list |
| Severe compliance incidents = 0 | Number | 0 in pilot | Incidents list |
| Platform uptime ≥ 99.5% | Percentage | 99.5% monthly | Ops monitoring |
| Mean incident resolution ≤ 4h | Number | ≤ 4 hours | Incidents list |
| Deadline adherence for critical tasks ≥ 90% | Percentage | 90% | KPI dashboard |
| Admin audit trail 100% coverage | Percentage | 100% of admin actions logged | Audit log verification |

---

### OKR 6: Build a Fundable Company (Goal Folder)

_Objective: Reach the milestones that prove Sanzu is a venture-backable business._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Seed round closed | Currency | €X raised | Fundraising list |
| Monthly burn rate on track | Currency | ≤ €X/month | Budget Tracking list |
| Core team positions filled | Number | X hires | Hiring Pipeline list |
| Key vendor/partner contracts signed | Number | ≥ X contracts | Contracts list |
| Board update cadence maintained | True/False | Monthly updates sent | Board Updates list |
| IP protections filed | Task | Trademark + domain secured | IP & Registrations list |

---

### OKR 7: Sustain a Healthy Innovation Pipeline (Goal Folder)

_Objective: Continuously discover, evaluate, and shape the next wave of value._

| Key Result (Goal Name) | Type | Target | Measured By |
|------------------------|------|--------|-------------|
| Ideas RICE-scored per month ≥ 5 | Number | ≥ 5/month | Under Evaluation list |
| PRD readiness pass rate ≥ 90% | Percentage | 90% | PRDs list |
| Opportunity slices A, B, C validated | Task | 3 slices validated | Opportunity Briefs list |
| Post-MVP epics scoped and estimated | Task | Epics 15-21 ready | Epics & Stories list |
| Feature request triage ≤ 7 days | Number | ≤ 7 days avg | Feature Requests list |

---

### How to Set Up OKRs in ClickUp Goals

1. **Create a Goal Folder** per Objective:
   - Go to **Sidebar → Goals → + New Folder**
   - Name it: e.g., "OKR 1: Deliver a Reliable Platform"
   - Set the time period (e.g., "Q1 2026")

2. **Create a Goal** per Key Result inside the folder:
   - Click **+ New Goal** inside the folder
   - Name it with the KR text (e.g., "Agency activation rate ≥ 80%")
   - Choose the target type:
     - **Number** — for counts, hours, days
     - **Percentage** — for rates and ratios
     - **Currency** — for financial targets
     - **True/False** — for binary milestones
     - **Task** — link to specific ClickUp tasks that must be completed
   - Set the target value
   - Set the due date (end of quarter or milestone date)

3. **Link tasks as targets** (for task-type KRs):
   - Inside the goal, click **+ Add Target → Tasks**
   - Select tasks from the relevant list (e.g., link all Epic 12-14 story tasks)
   - Progress auto-updates as tasks are completed

4. **Update number/percentage KRs manually**:
   - For KPIs from the API dashboard or external sources
   - Update weekly during sprint reviews or monthly during OKR check-ins

### OKR Cadence

| Cadence | Action | Where |
|---------|--------|-------|
| Weekly | Update KR progress during sprint review | Goal targets |
| Bi-weekly | Review OKR dashboard for risks | Company Overview dashboard |
| Monthly | Board update with OKR snapshot | Board Updates list |
| Quarterly | OKR retrospective + set next quarter | Goal Folders (archive old, create new) |

### Mapping to Sanzu KPI Tree

```
North Star: On-time Critical Milestone Completion Rate
├── Business Outcomes → OKR 3 (Agency Adoption) + OKR 6 (Fundable Company)
├── User Outcomes → OKR 2 (Prove User Value)
├── Operational Outcomes → OKR 5 (Trust & Compliance)
└── Commercial Outcomes → OKR 4 (Launch to Market) + OKR 3 (Adoption)

Supporting:
├── Engineering Health → OKR 1 (Reliable Platform)
└── Innovation Pipeline → OKR 7 (Healthy Pipeline)
```

---

## ClickUp Dashboards (Free Tier: up to 100 dashboards)

Dashboards are created manually in the ClickUp UI: **Sidebar → Dashboards → + New Dashboard**.

### Recommended Dashboards

#### 1. Sprint Dashboard (Engineering Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Sprint Burndown | Status chart | Current Sprint list |
| Stories by Status | Pie chart | Current Sprint list |
| Bugs Open | Count | Bug Triage list |
| Recent Commits | Activity feed | Engineering space |
| Sprint Goal Progress | Goals widget | Current sprint goal |

#### 2. Product Pipeline (Product & Strategy Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Ideas by Stage | Status chart | Ideas & Innovation lists |
| RICE Top 10 | Table | Under Evaluation list |
| PRD Status | Status chart | PRDs list |
| Epics Progress | Bar chart | Epics & Stories list |
| Opportunity Themes | Text block | Link to portfolio run |

#### 3. Release Tracker (Engineering Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Epic Completion % | Progress bar | All epic lists |
| Open Tech Debt | Count | Tech Debt list |
| Release Timeline | Gantt/timeline | Release Tracking list |
| Test Results | Text block | Latest CI summary |

#### 4. GTM & Revenue (Go-To-Market Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Prospects by Stage | Pipeline chart | Prospects list |
| Tenant Onboarding Progress | Status chart | Checklists list |
| MRR Goal | Goals widget | MRR goal |
| Launch Readiness | Checklist | Launch Plan list |

#### 5. Company Overview (Cross-Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Key Goals Progress | Goals widget | All goal folders |
| Team Size / Hiring | Count | Hiring Pipeline list |
| Budget vs Actuals | Text block | Finance lists |
| Support Ticket Volume | Count | Support Tickets list |
| Customer Health Summary | Status chart | Customer Health list |
| Upcoming Board Updates | Table | Board Updates list |

#### 6. Ops & Compliance (Governance Space)

| Widget | Type | Data Source |
|--------|------|-------------|
| Open Incidents | Count | Incidents list |
| Compliance Checklist | Status chart | GDPR & Phase Gates lists |
| Risk Register Heat Map | Table | Risk Register list |
| Uptime Goal | Goals widget | Uptime goal |

### How to Create Dashboards

1. Go to **Sidebar → Dashboards → + New Dashboard**
2. Name it (e.g., "Sprint Dashboard")
3. Click **+ Add Widget** and choose from: Status, Table, Chat, Goals, Time Tracking, Custom, etc.
4. Configure each widget's data source (select the relevant Space/Folder/List)
5. Arrange widgets by dragging them into position
6. Share with team members as needed

### Free Tier Dashboard Tips

- Use **Status widgets** (pie/bar charts) — available on all plans
- Use **Table widgets** to show task lists filtered by status
- Use **Text block widgets** as manual KPI displays (update weekly)
- Use **Goals widgets** to embed goal progress directly
- Avoid custom formula widgets (paid feature)
- Keep to ~6 focused dashboards to stay under limits comfortably
