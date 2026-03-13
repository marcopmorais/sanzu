# 23 — Backlog: Epics, Stories and Acceptance (Full)

## Epics by phase
### V1 Foundation
1) Auth + invitations
2) Case management
3) RBAC + audit
4) Questionnaire + branching
5) Workflow engine + dependencies
6) Vault + versioning + sensitivity tags
7) Template engine + PDF generation
8) Dashboard + progress + missing docs
9) Notifications-lite

### V2 Agency scale
10) Multi-case ops dashboard
11) SLA tracking + workload
12) Analytics

### V3 Automation
13) Notification rules engine
14) Connector framework
15) Assisted submissions + approvals + receipts

### V4 Mission control
16) Workstream orchestration
17) Evidence graph
18) Status polling + reconciliation
19) Evidence export (closure proof)

## Acceptance patterns (global)
- Every state change emits an AuditEvent
- RBAC tests for every endpoint + UI action
- Rules engine is deterministic and unit-tested
- Blocked→Ready transitions are explainable (“why blocked?”)
- Template generation validates required fields before PDF
