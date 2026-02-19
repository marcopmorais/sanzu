# Story 11.1: Export Case Audit and Evidence Package

Status: done

## Story

As an authorized agency oversight user,
I want to export a case audit and evidence package,
so that I can support compliance and operational reviews with verifiable records.

## Acceptance Criteria

1. Given a user has oversight permission, when they request an export for a case, then the export includes audit events, evidence references, and relevant status history.
2. And restricted content is excluded or redacted based on role and policy.
3. And export generation is recorded as an auditable event with a deterministic export identifier.

## Tasks / Subtasks

- [ ] Define export content contract (what is included, what is referenced) (AC: 1,2)
- [ ] Implement export generation pipeline (sync first, async if needed) (AC: 1,3)
- [ ] Implement redaction rules for restricted content (AC: 2)
- [ ] Add API endpoint to request and retrieve exports (AC: 1,3)
- [ ] Add UI surfaces to initiate and download export (AC: 1)
- [ ] Add tests
  - [ ] Role-based redaction tests (AC: 2)
  - [ ] Integration test for export creation and audit event (AC: 3)

## Dev Notes

- Prefer exporting references to evidence rather than bundling raw documents in initial iteration to reduce security risk and payload size.
- Ensure export is reproducible: same inputs produce same normalized content ordering (suitable for audits and comparisons).

### References

- PRD Post-MVP FR: `_bmad-output/definition/prd.md` (FR67)
- Epic definition: `_bmad-output/definition/epics_and_stories.md` (Story 11.1)
- Audit baseline: `_bmad-output/implementation-artifacts/6-1-review-case-audit-trail.md`
- Document evidence baseline: `_bmad-output/implementation-artifacts/4-1-upload-and-download-case-documents.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

