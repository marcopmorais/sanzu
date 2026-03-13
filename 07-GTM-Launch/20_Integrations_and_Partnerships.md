# 20 â€” Integrations and Partnerships

## Integration maturity ladder
| Level | Meaning | Example | Version |
|---|---|---|---|
| 0 None | upload + templates only | printable packages | V1 |
| 1 Assisted | Sanzu drafts, user submits | pre-filled portal forms | V3 |
| 2 Semi | API submission with approval | submit request + receipt | V3.1 |
| 3 Deep | status polling + reconciliation | bank claim status | V4 |

## Candidate partner surfaces
- Banks: notification + account closure workflows
- Insurers: claim initiation + status
- Employers/payroll: document requests
- Utilities/subscriptions: cancellation/transfer requests
- Government portals: only where feasible, with strict approval and evidence capture

## Integration requirements
- Consent/authorization records per action
- Receipt capture and auditable payload hash
- Retry logic and idempotent submission
- Status state machine per external party
