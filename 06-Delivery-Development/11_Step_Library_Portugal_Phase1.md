# 11 â€” Step Library (Portugal Phase 1)

## Scope & disclaimer
This library represents a **practical common-case** step set in Portugal for post-loss bureaucracy.
It is **not legal advice** and must be validated with PT-qualified experts before production.

## Step metadata
- step_key (stable identifier)
- title
- owner_type: Agency | Family
- criticality: mandatory | optional
- triggers (boolean logic)
- prerequisites (step_keys)
- required_docs (doc_types)
- outputs (confirmations / templates / receipts)
- completion_rule: manual | evidence-based | hybrid
- notes / variants

---

## WORKSTREAM A â€” Core registration & proof (MANDATORY)

### A1 core_medical_death_confirmation
- title: Obtain medical death confirmation
- owner_type: Agency
- criticality: mandatory
- triggers: always
- prerequisites: â€”
- required_docs: medical_death_confirmation
- outputs: proof_ready_for_registration
- completion_rule: evidence-based
- notes: source differs by setting (hospital/home)

### A2 core_death_registration_proof
- title: Register death and obtain official proof
- owner_type: Agency
- criticality: mandatory
- triggers: always
- prerequisites: core_medical_death_confirmation
- required_docs: medical_death_confirmation, deceased_id_document (if available)
- outputs: death_registration_proof
- completion_rule: hybrid (evidence + manual confirm)

### A3 core_certified_copies
- title: Collect certified copies (as needed)
- owner_type: Agency
- criticality: optional (recommended)
- triggers: if many institutions OR agency_policy.certified_copies=true
- prerequisites: core_death_registration_proof
- required_docs: death_registration_proof
- outputs: certified_copies_available
- completion_rule: manual

---

## WORKSTREAM B â€” Family onboarding & baseline evidence (MANDATORY)

### B1 family_invite_and_roles
- title: Invite family and assign Editor
- owner_type: Agency
- criticality: mandatory
- triggers: always
- prerequisites: â€”
- required_docs: â€”
- outputs: invites_sent, editor_assigned
- completion_rule: evidence-based (invite accepted)

### B2 family_intake_questionnaire
- title: Complete intake questionnaire
- owner_type: Family
- criticality: mandatory
- triggers: always
- prerequisites: family_invite_and_roles
- required_docs: â€”
- outputs: questionnaire_payload_v1
- completion_rule: evidence-based

### B3 family_upload_baseline_docs
- title: Upload baseline identity/relationship documents
- owner_type: Family
- criticality: mandatory
- triggers: always
- prerequisites: family_intake_questionnaire
- required_docs:
  - editor_id_document
  - deceased_id_document (if available)
  - relationship_proof (if applicable)
  - tax_id_numbers (captured as fields; docs optional)
- outputs: baseline_evidence_set
- completion_rule: evidence-based

---

## WORKSTREAM C â€” Banks & accounts (COMMON, TRIGGERED)

### C1 bank_identify_accounts
- title: Identify banks/accounts
- owner_type: Family
- criticality: optional
- triggers: has_banks == yes OR has_banks == unknown
- prerequisites: family_intake_questionnaire
- required_docs: bank_statements_optional
- outputs: bank_list
- completion_rule: evidence-based (bank list present)

### C2 bank_prepare_notification_package
- title: Prepare bank notification package
- owner_type: Agency
- criticality: optional
- triggers: has_banks == yes OR bank_list not empty
- prerequisites: core_death_registration_proof, bank_identify_accounts, family_upload_baseline_docs
- required_docs: death_registration_proof, relationship_proof
- outputs: generated_docs: tmpl_bank_notification_letter
- completion_rule: manual

### C3 bank_track_responses
- title: Track bank responses and required next steps
- owner_type: Agency
- criticality: optional
- triggers: package_sent == true
- prerequisites: bank_prepare_notification_package
- required_docs: submission_receipts_optional
- outputs: per_bank_status
- completion_rule: manual

---

## WORKSTREAM D â€” Insurance (TRIGGERED)

### D1 insurance_identify_policies
- title: Identify insurance policies
- owner_type: Family
- criticality: optional
- triggers: has_insurance == yes OR has_insurance == unknown
- prerequisites: family_intake_questionnaire
- required_docs: policy_docs_optional
- outputs: insurer_list
- completion_rule: evidence-based

### D2 insurance_prepare_claim_package
- title: Prepare insurance claim package
- owner_type: Agency
- criticality: optional
- triggers: insurer_list not empty
- prerequisites: core_death_registration_proof, insurance_identify_policies, family_upload_baseline_docs
- required_docs: death_registration_proof, relationship_proof, policy_docs_optional
- outputs: generated_docs: tmpl_insurance_claim_request
- completion_rule: manual

### D3 insurance_track_claim_status
- title: Track claim status
- owner_type: Agency
- criticality: optional
- triggers: claim_initiated == true
- prerequisites: insurance_prepare_claim_package
- required_docs: submission_receipts_optional
- outputs: claim_status_updates
- completion_rule: manual

---

## WORKSTREAM E â€” Benefits / pension (TRIGGERED)

### E1 benefits_assess_and_select
- title: Assess benefits/pension presence
- owner_type: Family
- criticality: optional
- triggers: has_benefits == yes OR has_benefits == unknown
- prerequisites: family_intake_questionnaire
- required_docs: â€”
- outputs: benefits_scope
- completion_rule: evidence-based

### E2 benefits_notify_entities
- title: Notify relevant entities (as applicable)
- owner_type: Agency
- criticality: optional
- triggers: benefits_scope not empty
- prerequisites: core_death_registration_proof, family_upload_baseline_docs
- required_docs: death_registration_proof, relationship_proof
- outputs: notification_confirmations
- completion_rule: manual

### E3 benefits_collect_claim_docs
- title: Collect claim documents
- owner_type: Family
- criticality: optional
- triggers: benefits_notification_started == true
- prerequisites: benefits_notify_entities
- required_docs: bank_account_proof (beneficiary), additional_docs_optional
- outputs: claim_ready_package
- completion_rule: evidence-based

---

## WORKSTREAM F â€” Employer / payroll (TRIGGERED)

### F1 employment_notify_employer
- title: Notify employer / request documents
- owner_type: Family
- criticality: optional
- triggers: has_employer == yes
- prerequisites: core_death_registration_proof
- required_docs: death_registration_proof
- outputs: employer_confirmation, generated_docs(optional): tmpl_employer_notification
- completion_rule: manual

---

## WORKSTREAM G â€” Utilities/subscriptions (OPTIONAL COMMON)

### G1 services_identify_recurring
- title: Identify recurring services
- owner_type: Family
- criticality: optional
- triggers: has_services == yes OR has_services == unknown
- prerequisites: family_intake_questionnaire
- required_docs: â€”
- outputs: services_list
- completion_rule: evidence-based

### G2 services_generate_requests
- title: Generate cancellation/transfer requests
- owner_type: Agency
- criticality: optional
- triggers: services_list not empty
- prerequisites: services_identify_recurring, core_death_registration_proof
- required_docs: death_registration_proof
- outputs: generated_docs: tmpl_service_cancellation_request
- completion_rule: manual

---

## WORKSTREAM H â€” Closure (MANDATORY)

### H1 closure_review_and_archive
- title: Closure review and archive
- owner_type: Agency
- criticality: mandatory
- triggers: always
- prerequisites: core_death_registration_proof, family_upload_baseline_docs
- required_docs: â€”
- outputs: case_closed, closure_summary
- completion_rule: manual (with audit)
