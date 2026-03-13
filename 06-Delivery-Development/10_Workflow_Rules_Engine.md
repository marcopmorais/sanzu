# 10 â€” Workflow Rules Engine

## Purpose
Generate a **case-specific plan** from inputs:
- steps + owners
- dependencies (data, evidence, step)
- required docs/evidence
- closure criteria

This is the backbone that agentic behavior builds on.

## Inputs (V1)
- identity: deceased name/date, editor identity
- presence flags: banks, insurance, benefits, employer, services (yes/no/unknown)
- agency policy toggles: certified copies, default workstreams, retention

## Step schema (library)
- step_key, title, description
- owner_type: Agency|Family
- triggers: boolean logic on inputs
- prerequisites: step_keys
- required_docs: doc_types
- outputs: confirmations/templates/receipts
- criticality: mandatory|optional
- completion_rule: manual|evidence-based|hybrid

## Dependency types
- Data dependency: questionnaire fields
- Evidence dependency: uploaded docs / receipts
- Step dependency: other steps completed

## Status automation
- If required evidence missing â†’ Blocked
- If prerequisites satisfied â†’ Ready
- InProgress when user starts work
- Completion rules:
  - V1: manual completion (default)
  - V2+: evidence-based completion for low-risk steps

## Branching examples
- has_banks=yes or unknown â†’ include bank workstream and prompt for list
- has_insurance=yes or unknown â†’ include insurance workstream and prompt for policies
- has_employer=yes â†’ include employer workstream
- has_services=yes or unknown â†’ include services workstream

## Closure criteria (V1)
- all mandatory steps completed OR manager force-close with reason + audit (no critical blockers).
