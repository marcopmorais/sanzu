# 08 â€” Non-Functional Requirements

## Security & privacy
- TLS in transit; encryption at rest
- Signed URLs for file access (short expiry)
- RBAC with least privilege
- Audit trail for all state changes and sensitive actions
- Sensitive docs tagged `restricted` with stronger access rules
- MFA for agency admins (recommended)
- Data minimization (collect only whatâ€™s needed)

## Reliability
- 99.5% uptime (V1 target)
- Backups + restore tested
- PDF generation jobs are retryable and idempotent
- Graceful degradation for notification failures

## Performance
- Dashboard <2 seconds typical
- Upload supports retry/resume (nice-to-have early)
- PDF generation <10 seconds typical

## Compliance baseline (draft)
- GDPR: transparency, access/export, deletion path, retention controls
- DPA for agency customers; subprocessors list
