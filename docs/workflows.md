# Workflows

## SYNC_STATE

- Purpose: Sync PDLC status and local UX feedback artifacts into `_bmad-output/status/current_state.md`.
- Location: `_bmad/bmm/workflows/sync-state/workflow.md`
- Notes: ClickUp and Figma integrations are removed. Use local artifacts only.

## PDLC_MASTER

- Purpose: Assess PDLC maturity, run gate checks, and produce `_bmad-output/status/pdlc_state.md`.
- Location: `_bmad/bmm/workflows/pdlc-master/workflow.md`
- Notes: Includes a read-only CIS Strategic Review layer (innovation strategist, design thinking coach, creative problem solver, storyteller) and forbids CIS artifact generation during this phase.
- Notes: ClickUp and Figma integrations are removed. Do not execute external integration tool calls in this workflow.

## TESTARCH_CI

- Purpose: Provide early CI/CD signal for Sanzu with .NET quality checks, burn-in stability runs, and Azure App Service deployment.
- Location: `.github/workflows/ci.yml`
- Notes: Deployment is gated by Azure repository secrets/variables documented in `docs/ci-secrets-checklist.md`.
