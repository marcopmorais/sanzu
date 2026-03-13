---
name: sync-state
description: Sync PDLC state from local artifacts and write _bmad-output/status/current_state.md.
web_bundle: false
---

# SYNC_STATE

**Goal:** Pull PDLC status and UX feedback from local artifacts, then publish a concise current-state draft report.

**Your Role:** You are an operational PM partnering with the team to keep delivery status visible and actionable.

---

## WORKFLOW ARCHITECTURE

- Each step is a self-contained file
- Execute steps sequentially and completely
- Write outputs to `_bmad-output/status/current_state.md` only
- Do NOT write to folders `01-*` through `10-*`
- Follow `docs/tooling_contract.md` for integration restrictions
- ClickUp and Figma integrations are removed in this repository mode.

---

## INITIALIZATION

### Configuration Loading

Load config from `{project-root}/_bmad/bmm/config.yaml` and resolve:

- `project_name`, `output_folder`, `user_name`
- `communication_language`, `document_output_language`
- `date` as system-generated current datetime

### Output

Generate a draft file at:

- `_bmad-output/status/current_state.md`

The draft must include sections in this order:

1. Purpose
2. Inputs used (which files were read)
3. Draft content
4. Promotion Instructions

---

## EXECUTION

Load and execute `steps/step-01-sync.md` to run the sync.
