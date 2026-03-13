# 09 — Agentic Architecture

## Why agentic (and why not immediately)
Agentic behavior is valuable only after deterministic correctness is stable; otherwise it amplifies errors.

## Layered architecture
1) **Deterministic core**
- case data model
- step library + rules engine
- templates + validations

2) **Orchestration agents**
- plan synthesis, next-best-step, risk flags
- safe automations (bounded)

3) **Integrations layer**
- connectors, assisted submissions
- receipt capture, status polling

4) **Evidence graph (V4)**
- nodes: facts, docs, confirmations, requests
- edges: supports/depends_on/confirms/generated_from

5) **Governance layer**
- template versions, policy checks, auditability

## Safety patterns
- Suggest → Draft → Ask approval → Execute
- External actions always:
  - require explicit approval (role-bound)
  - generate a receipt record
  - are fully auditable

## Operational objects
- Goal: “Close case”
- Workstreams: registry, benefits, banks, insurance, employer, services
- Evidence: required docs + confirmations per step/workstream
