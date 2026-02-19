# Story 10.1: Define and Version Agency Playbooks

Status: done

## Story

As an Agency Administrator,
I want to define and version playbooks (templates and defaults),
so that new cases follow consistent agency-approved handling patterns.

## Acceptance Criteria

1. Given an agency administrator is configuring the tenant, when they create or update a playbook, then the playbook is versioned and retained for auditability.
2. And changes can be reviewed before becoming the default for new cases.
3. And playbook management is protected by tenant admin permissions and is audit logged.

## Tasks / Subtasks

- [ ] Define playbook domain model (versioning, status, effective date, author) (AC: 1,2,3)
- [ ] Implement persistence and API contracts for playbook CRUD + versioning (AC: 1,2,3)
- [ ] Implement "activate playbook version" workflow with review step (AC: 2,3)
- [ ] Implement frontend settings routes for playbook list and detail (AC: 2)
- [ ] Add tests
  - [ ] Validation and authorization tests (AC: 3)
  - [ ] Versioning behavior tests (AC: 1,2)
  - [ ] Playwright E2E for create -> review -> activate (AC: 2)

## Dev Notes

- Playbooks should apply to newly created cases only (Story 10.2), unless explicitly supported via migration tooling later.
- Keep playbook content bounded: avoid embedding free-form executable logic; prefer structured defaults that map to existing tenant/case configuration.

### References

- PRD Post-MVP FR: `_bmad-output/definition/prd.md` (FR66)
- Epic definition: `_bmad-output/definition/epics_and_stories.md` (Story 10.1)
- Authorization baseline: `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` (TenantAdmin policy)
- Frontend expectations: `_bmad-output/definition/prd-frontend.md` (Epic 10 routes)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

