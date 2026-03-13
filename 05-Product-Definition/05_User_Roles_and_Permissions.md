# 05 — User Roles and Permissions

## Human roles (fixed)
| Role | Actor | Purpose |
|---|---|---|
| Process Manager | Funeral agency | orchestrate and support |
| Editor | Family (designated) | upload docs, provide data, run steps |
| Reader | Family | view-only transparency |

## Constraints
- One agency org per case
- One Editor per case (reassignable by Manager)
- Unlimited Readers
- No “multiple agencies per case”

## Permission matrix
| Capability | Manager | Editor | Reader |
|---|:---:|:---:|:---:|
| Create case | ✅ | ❌ | ❌ |
| Invite users | ✅ | ❌ | ❌ |
| Assign/reassign Editor | ✅ | ❌ | ❌ |
| Upload docs | ✅ | ✅ | ❌ |
| Generate docs | ✅ | ✅ | ❌ |
| Update step status | ✅ | ⚠️ (family-owned steps) | ❌ |
| Close/archive case | ✅ | ❌ | ❌ |
| View dashboard | ✅ | ✅ | ✅ |
| Download shared docs | ✅ | ✅ | ✅ |

## Sensitive documents
- Tag documents as `restricted` to limit access to Manager + Editor only.

## Audit (minimum)
Log with timestamp, actor, case_id, metadata:
- case_created, invite_sent/accepted, role_changed
- document_uploaded/downloaded, template_generated
- step_status_changed, external_request_* (V3+), case_closed
