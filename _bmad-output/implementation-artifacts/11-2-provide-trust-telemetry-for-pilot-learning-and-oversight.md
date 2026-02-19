# Story 11.2: Provide Trust Telemetry for Pilot Learning and Oversight

Status: done

## Story

As an authorized oversight or platform user,
I want measurable trust telemetry for cases and workflows,
so that I can detect friction, blocked-step patterns, and outcome risks early.

## Acceptance Criteria

1. Given operational telemetry is available for workflow events, when a user views trust telemetry summaries, then the system shows reason-coded counts and outcome signals (e.g., time-to-first-action, blocked-resolution time).
2. And telemetry views support filtering by tenant, cohort, and time period.
3. And telemetry never exposes restricted case content and is accessible only to authorized roles.

## Tasks / Subtasks

- [ ] Define telemetry events and aggregation metrics (reason-coded, stable keys) (AC: 1)
- [ ] Implement storage and aggregation queries for telemetry summaries (AC: 1,2)
- [ ] Add API endpoints for telemetry summaries and drilldowns (AC: 2,3)
- [ ] Add UI surfaces for telemetry dashboards (tenant-level and platform-level as applicable) (AC: 1,2)
- [ ] Add tests
  - [ ] Aggregation correctness tests (AC: 1,2)
  - [ ] Authorization and redaction tests (AC: 3)

## Dev Notes

- Reuse or align with KPI mechanisms where possible (Epic 7), but keep trust telemetry semantics separate from business KPIs.
- Reason codes should align with Epic 9 blocked reasons to enable cross-epic analysis.

### References

- PRD Post-MVP FR: `_bmad-output/definition/prd.md` (FR68)
- Epic definition: `_bmad-output/definition/epics_and_stories.md` (Story 11.2)
- KPI governance baseline: `_bmad-output/implementation-artifacts/7-4-view-kpi-dashboard-with-drilldown.md`
- Frontend expectations: `_bmad-output/definition/prd-frontend.md` (Epic 11 routes)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

