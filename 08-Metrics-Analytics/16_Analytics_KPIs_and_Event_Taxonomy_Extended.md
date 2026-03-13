# 16 â€” Analytics, KPIs and Event Taxonomy (Extended)

## KPI tree
- **North Star:** completed cases/month with high satisfaction
- Adoption: active agencies, active managers
- Engagement: steps completed/case, docs uploaded/case, templates generated/case
- Efficiency: median cycle time; time-to-next-action; blockers age
- Quality: rework rate; missing-doc loops; step reopen rate
- Satisfaction: Family NPS; Agency NPS; â€œstatus calls reducedâ€ (survey)
- Economics: support minutes/case; infra cost/case
- Revenue: revenue/case; agency retention

## Event taxonomy (minimum viable)
Naming: `object_action`

### Core events (V1)
- case_created
- invite_sent, invite_accepted
- role_changed
- questionnaire_started, questionnaire_completed
- step_viewed, step_status_changed
- document_uploaded, document_downloaded
- template_generated
- case_closed
- nps_submitted

### Extended events (V2â€“V4)
- workstream_created, workstream_closed
- external_request_created, external_request_approved, external_request_submitted
- external_request_status_updated
- evidence_item_linked
- risk_flag_raised, risk_flag_resolved

## Required properties (examples)
- case_id, org_id, actor_role
- step_key, from_status, to_status
- doc_type, sensitivity, size_bytes
- template_id
- external_party_type, destination, receipt_reference
