# 24 — Testing, QA, Security and Runbooks

## Test layers
- Unit: RBAC, rules engine, template validations
- Integration: uploads (signed URLs), PDF job processing
- E2E: create case → invite → intake → upload → generate → close
- Security: access control tests; signed URL expiry; audit integrity

## Runbooks (minimum)
### Incident: unauthorized access attempt
1) confirm logs/correlation IDs
2) revoke sessions / rotate keys if required
3) assess scope; notify per policy
4) remediation + postmortem

### Incident: PDF generation failures
1) inspect job queue + worker health
2) retry with idempotency
3) degrade gracefully: allow “download raw data” fallback
4) monitor error rate

### Data restore
- restore last backup, validate integrity, run reconciliation, verify audit continuity
