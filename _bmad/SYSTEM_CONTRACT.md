# SYSTEM CONTRACT

## Write Scope (Hard Rule)

- The folders `01-*` through `10-*` are READ-ONLY for agents.
- Agents may ONLY write to:
  - `_bmad-output/`
  - `_bmad/`
  - `docs/`
- If asked to change anything in `01` through `10`, generate a draft under `_bmad-output` and include "Promotion Instructions" instead.

## Artifact Draft Convention

- Any PDLC artifact draft must be written to:

  `_bmad-output/<phase>/<artifact_name>.md`

- Each draft must include the following sections:
  - Purpose
  - Inputs used (which files were read)
  - Draft content
  - Promotion Instructions (target location in `01` through `10`)

## Integrations

- **ClickUp integrations are ENABLED.** Use `mcp__claude_ai_ClickUp__clickup_*` MCP tools for PDLC status tracking, idea capture, and document management.
- Figma integrations are removed from PDLC workflows.
- Follow the Dual-Write Rule in `docs/tooling_contract.md`: write detailed content to local files, sync status and summaries to ClickUp.
- See `docs/pdlc/ClickUp_Workflow_Guide.md` for agent-specific ClickUp workflows.
