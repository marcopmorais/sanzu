# ClickUp OS Policy
# Applies to all Sanzu BMAD agents — enforced automatically at session start

ClickUp is the persistent memory and project operating system for all Sanzu BMAD sessions.
Every agent reads state from ClickUp before acting and writes results back to ClickUp after acting.
No agent ends a session without updating task state.

---

## 1. Identity Declaration (start of every session)

At the start of every BMAD session, declare the following to ClickUp via a comment on the Session Log task:

```
Agent: {agent_name}
Session ID: {ISO_timestamp}  (e.g. 2026-03-08T14:30:00Z)
Active PDLC Phase: {phase_name}
```

The Session Log task lives in the Governance & Ops space. Create it if it does not exist (name: "BMAD Session Log", list: Knowledge OS 901521448031).

---

## 2. Read Rules (before acting)

**Before creating any task:**
- Search ClickUp for existing tasks with similar name/description to avoid duplicates.
- Use `clickup_search` with the task name as the query before calling `clickup_create_task`.
- If a duplicate is found, update the existing task instead of creating a new one.

**Before executing any work:**
- Read the relevant task using `clickup_get_task` to load full context and comment history.
- Never reconstruct state from memory alone — always call the MCP tool.

**Before sprint planning:**
- Read all tasks in Current Sprint (list 901521447990) across all statuses.
- Read Sprint Backlog (list 901521447991) filtered to "To Do" status.
- Use `clickup_get_list` or iterate via `clickup_search` within those list IDs.

---

## 3. Write Rules (while acting)

### CREATE a task

Use `clickup_create_task`. Structure every task as follows:

**Name:** `[AGENT_CODE] Clear, action-oriented description`
(e.g. `[PM] Draft PRD for Funeral Home Onboarding flow`)

**Description must include:**
```
## Context
[Why this task exists — what problem it solves or what artifact it produces]

## Objective
[Specific goal for this task — what done looks like]

## Acceptance Criteria
- [ ] AC 1
- [ ] AC 2
- [ ] AC 3

## Expected Artifact
[File path or ClickUp link to the output artifact]
```

**Custom Fields (set where available):**
- `agent_name`: the agent's code (e.g. PM, DEV, ARX, TEA, SM, UX, ANA, TW)
- `pdlc_phase`: current PDLC phase number and name (e.g. "3 — Solutioning")
- `confidence_pct`: 0–100 integer representing agent's confidence in the output
- `session_id`: ISO timestamp of the current session

**Default list assignment:**
| Work type | Target list |
|-----------|-------------|
| Sprint story | Current Sprint 901521447990 |
| Backlog story | Sprint Backlog 901521447991 |
| PRD | PRDs 901521447940 |
| Epic / Story artifact | Epics & Stories 901521447942 |
| Architecture decision | Architecture Decisions 901521447975 |
| ADR | ADRs 901521448035 |
| Market research | Market Research 901521447929 |
| Bug | Bug Triage 901521447994 |
| Knowledge artifact | Knowledge OS 901521448031 |

### UPDATE task when starting work

Call `clickup_update_task`:
- Set `status` to `"in progress"`

Call `clickup_create_task_comment`:
- Comment body:
  ```
  [IN PROGRESS] Session {session_id}
  Reasoning: {why_this_approach}
  Progress: {what_is_being_done_now}
  ```

### DELIVER a task when done

Call `clickup_update_task`:
- Set `status` to `"in review"` if human approval is needed
- Set `status` to `"done"` if the agent can autonomously confirm completion
- Update `confidence_pct` custom field with final score

Call `clickup_create_task_comment`:
- Comment body:
  ```
  [DELIVERED] Session {session_id}
  Summary: {one_paragraph_of_what_was_done}
  Artifact: {local_file_path_or_link}
  Confidence: {confidence_pct}%
  ```

### BLOCK a task if stuck

Call `clickup_update_task`:
- Set `status` to `"blocked"`

Call `clickup_create_task_comment`:
- Comment body:
  ```
  [BLOCKED] Session {session_id}
  Blocker: {exact_description_of_what_is_blocking}
  What is needed to unblock: {specific_action_or_information_needed}
  Attempted: {what_was_tried}
  ```

---

## 4. End of Session Protocol

Before ending any session, complete ALL of the following steps in order:

**Step 1 — Verify task states**
For every task touched in this session:
- Confirm status is updated (not stuck on "in progress").
- Confirm artifact is linked in the task description or a comment.

**Step 2 — Close or park incomplete work**
If any work item is incomplete:
- Create a new "To Do" task in the appropriate list (see table above).
- Include full context: what was done, what inputs are available, what the next step is.
- Do NOT leave work in a mentally-only state — ClickUp is the source of truth.

**Step 3 — End-of-session comment**
Add a final comment to each task that was active in this session:
```
[SESSION END] {ISO_timestamp}
Status at close: {status}
Next step: {specific_description_of_what_to_do_next}
Artifact location: {path_or_link}
```

**Step 4 — Update Session Log**
Post the full session summary to the Session Log task as a comment.

---

## 5. ClickUp Space Reference — Sanzu Workspace

### Spaces

| Space | Purpose | ID |
|-------|---------|-----|
| Product Strategy | Discovery, PRDs, market research | 901510198367 |
| Engineering | Architecture, implementation, bugs | 901510198384 |
| UX Design | UX plans, wireframes, design specs | 901510198393 |
| Go to Market | GTM strategy, launch planning | 901510198395 |
| Governance & Ops | Audit, compliance, session logs | 901510198473 |

### Critical Lists

| List | Purpose | ID |
|------|---------|-----|
| Current Sprint | Active sprint stories | 901521447990 |
| Sprint Backlog | Groomed stories ready to pull | 901521447991 |
| PRDs | Product Requirements Documents | 901521447940 |
| Epics & Stories | Epic decompositions and story files | 901521447942 |
| Architecture Decisions | Architecture and design decisions | 901521447975 |
| ADRs | Architecture Decision Records | 901521448035 |
| Knowledge OS | Session logs, reference artifacts | 901521448031 |
| Bug Triage | Bugs and defects | 901521447994 |
| Market Research | Research reports and analysis | 901521447929 |
| Idea Intake | Phase 0 candidate ideas (Innovation Idea Management template) | 901521947569 |

---

## 6. MCP Tool Reference

All ClickUp operations MUST use MCP tool calls. Never assume or reconstruct state.

| Operation | MCP Tool |
|-----------|---------|
| Search tasks | `clickup_search` |
| Get task details | `clickup_get_task` |
| Get list tasks | `clickup_get_list` |
| Create task | `clickup_create_task` |
| Update task status/fields | `clickup_update_task` |
| Add comment | `clickup_create_task_comment` |
| Get comments | `clickup_get_task_comments` |
| Find member by name | `clickup_find_member_by_name` |
| Get workspace members | `clickup_get_workspace_members` |

**On MCP tool failure:**
1. STOP the current operation.
2. Log the error with full context.
3. Create a "Blocked" task describing the failure (use local write if ClickUp is unreachable).
4. Do NOT proceed with work that depends on the failed ClickUp state.
