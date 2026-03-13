# 13 â€” Data Model (Extended)

## Core principle
Sanzu is **case-centric**. Everything attaches to `case_id`. Multi-tenancy is enforced via `org_id` on Case.

## V1 core entities
- Organization (funeral agency tenant)
- User
- Case
- CaseParticipant (role per case)
- QuestionnaireResponse (versioned)
- WorkflowStepInstance (generated from Step Library)
- Document + DocumentVersion (vault)
- Template + GeneratedDocument
- AuditEvent
- NotificationPreference

## V2â€“V4 extensions
- Workstream (banks/insurance/benefits/employer/services)
- ExternalParty (bank, insurer, employer, utility)
- ExternalRequest (create â†’ approve â†’ submit)
- ExternalRequestStatus (polling + reconciliation)
- EvidenceItem (doc/receipt/confirmation)
- PolicyRule (compliance checks)
- TaskSLA (time-to-next-action; blockers)

## Key fields (draft)
### Case
- case_id, org_id, status (Draft/Active/Closing/Archived)
- deceased_full_name, date_of_death, municipality(optional)
- created_at, updated_at

### WorkflowStepInstance
- step_instance_id, case_id, step_key
- owner_type (Agency/Family), criticality
- status (NotStarted/Blocked/Ready/InProgress/Completed)
- prerequisites_satisfied (bool)
- required_docs_satisfied (bool)

### Document
- doc_id, case_id, doc_type, sensitivity (normal/restricted)
- latest_version_id, created_by, created_at

### GeneratedDocument
- generated_doc_id, case_id, template_id
- output_doc_id (points to vault Document)
- field_hash (for reproducibility), created_at

### AuditEvent
- event_id, case_id, actor_user_id, event_type, metadata_json, created_at

## Evidence graph (V4 concept)
- Node types: Fact, Document, Receipt, Confirmation, Status
- Edge types: supports, depends_on, confirms, generated_from
