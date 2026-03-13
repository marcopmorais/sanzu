# 02 â€” Grand Vision: Agentic Mission Control Platform

## Destination
Sanzu.ai becomes the **agentic mission control platform** for post-loss administrative transition:
- an â€œoperations roomâ€ for family + funeral agency,
- orchestrating parallel workstreams (registry, benefits, banks, insurance, employer, services),
- with agents that plan, draft, request approvals, execute low-risk automations, and later submit actions through integrations,
- while maintaining strict role boundaries, audit trails, and privacy controls.

## What â€œmission controlâ€ means
| Layer | V1 (Copilot) | V4 (Mission control) |
|---|---|---|
| Planning | deterministic checklist | adaptive plan + workstreams |
| Execution | user-driven | semi-autonomous with approvals |
| Data | form inputs + uploads | living case graph (facts/evidence/outcomes) |
| Coordination | shared status | orchestration with SLAs & blockers |
| Compliance | template governance | policy checks + evidence-based audit |
| Integrations | minimal | assisted submissions + status polling |

## Internal agents (capabilities)
These are system components (not user roles).
| Agent | Purpose | Examples |
|---|---|---|
| Intake Agent | Collect missing info with minimal friction | progressive questions, validation |
| Planning Agent | Build the best plan | select/sequence steps, owners |
| Document Agent | Generate & validate docs | pre-fill, completeness checks, PDF |
| Evidence Agent | Link evidence to steps | Blockedâ†’Ready transitions |
| Notification Agent | Useful nudges | reminders only when unblocked |
| Integration Agent | Partner interactions | submissions/requests + receipts |
| Risk/Compliance Agent | Detect issues | inconsistent IDs, missing approvals |

## Trust boundaries
- Never perform legally binding actions without explicit authorization.
- Autonomy is phased:
  - V1: deterministic workflows + templates
  - V2: recommendations + low-risk auto transitions
  - V3: assisted submissions (approval-based) + receipts
  - V4: orchestration + status reconciliation (evidence graph)

## Durable moats
- Portugal-tuned step/template library + governance
- Evidence graph that defines â€œdoneâ€ with proof
- Distribution via agencies and operational lock-in
- Analytics on bottlenecks (cycle time, rework) to continuously optimize
