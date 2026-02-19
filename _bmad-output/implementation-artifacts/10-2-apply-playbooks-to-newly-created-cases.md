# Story 10.2: Apply Playbooks to Newly Created Cases

Status: done

## Story

As a Process Manager,
I want new cases to start with an agency playbook applied,
so that workflows, defaults, and templates are predictable.

## Acceptance Criteria

1. Given a new case is created under a tenant with an active playbook, when case initialization completes, then playbook defaults are applied to the case.
2. And the applied playbook version is recorded in the case timeline.
3. And if no playbook is active, the system applies standard tenant defaults without failure.

## Tasks / Subtasks

- [ ] Extend case creation workflow to resolve active playbook version (AC: 1,3)
- [ ] Apply playbook defaults during case initialization (AC: 1)
- [ ] Record applied playbook version as audit/timeline event (AC: 2)
- [ ] Expose applied playbook version in case details response (AC: 2)
- [ ] Add tests
  - [ ] Unit tests for playbook resolution and fallbacks (AC: 1,3)
  - [ ] Integration test for case create with active playbook and timeline event (AC: 1,2)

## Dev Notes

- Keep the application deterministic: given tenant + active playbook version, the applied defaults should be reproducible for audit and support diagnostics.

### References

- PRD Post-MVP FR: `_bmad-output/definition/prd.md` (FR66)
- Epic definition: `_bmad-output/definition/epics_and_stories.md` (Story 10.2)
- Case creation baseline: `_bmad-output/implementation-artifacts/2-1-create-case-with-required-metadata.md`
- Audit/timeline baseline: `_bmad-output/implementation-artifacts/3-5-provide-timeline-ownership-and-notifications.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

