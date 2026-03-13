# 12 â€” Document Template Catalogue (Multi-Version)

## Template metadata
- template_id
- name
- purpose
- used_in_steps
- required_fields (minimum)
- version_introduced
- notes (variants, compliance)

> Templates must be validated by PT-qualified experts before production.

---

## V1.0 â€” Foundation templates (ship with pilot)

### tmpl_bank_notification_letter
- name: Bank notification letter (generic)
- purpose: Notify a bank and request next steps / required documents
- used_in_steps: bank_prepare_notification_package
- required_fields:
  - deceased_full_name, date_of_death
  - deceased_id (if available)
  - requester_name, requester_id, relationship
  - contact_email/phone, address(optional)
- version_introduced: V1.0
- notes: bank-specific variants later

### tmpl_insurance_claim_request
- name: Insurance claim request (generic)
- purpose: Initiate claim process with insurer
- used_in_steps: insurance_prepare_claim_package
- required_fields:
  - deceased_full_name, date_of_death
  - policy_number(optional)
  - claimant_name/id/relationship
  - bank_details(optional)
- version_introduced: V1.0

### tmpl_service_cancellation_request
- name: Service cancellation/transfer request
- purpose: Cancel or transfer utilities/subscriptions
- used_in_steps: services_generate_requests
- required_fields:
  - provider_name, contract_reference(optional)
  - deceased identity, date_of_death
  - requester identity + contact
- version_introduced: V1.0

### tmpl_case_document_checklist
- name: Case document checklist cover
- purpose: Printable checklist for the case (offline execution)
- used_in_steps: dashboard (supporting artefact)
- required_fields:
  - case_id, generated_at
  - required_doc_types list
- version_introduced: V1.0

---

## V1.1 â€” Correctness + breadth

### tmpl_request_certified_copies
- name: Request certified copies (generic)
- purpose: Standard request document for certified copies where applicable
- used_in_steps: core_certified_copies
- required_fields:
  - deceased identity, registry reference(optional)
  - requester identity
- version_introduced: V1.1

### tmpl_employer_notification
- name: Employer notification letter (generic)
- purpose: Notify employer and request final declarations/entitlements
- used_in_steps: employment_notify_employer
- required_fields:
  - employer_name
  - deceased identity + date_of_death
  - requester identity + relationship
- version_introduced: V1.1

---

## V2.0 â€” Agency scale ops

### tmpl_agency_case_summary
- name: Agency internal case summary
- purpose: Operational snapshot for handoff/workload
- used_in_steps: agency dashboards
- required_fields:
  - case_status, blockers, next_actions
  - key contacts
- version_introduced: V2.0

---

## V3.0 â€” Automation & assisted submissions

### tmpl_submission_receipt_record
- name: Submission receipt record (audit evidence)
- purpose: Store proof of submission/requests via connectors
- used_in_steps: external_request_submitted flows
- required_fields:
  - destination, timestamp
  - payload_hash, approval_actor
  - receipt_reference / attachment(optional)
- version_introduced: V3.0

---

## V4.0 â€” Mission control (evidence graph)

### tmpl_case_evidence_export
- name: Evidence graph export
- purpose: Export structured â€œproof of completionâ€ package
- used_in_steps: closure_review_and_archive (V4)
- required_fields:
  - evidence nodes, linked docs, confirmations, statuses
- version_introduced: V4.0
