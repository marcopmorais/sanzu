# Platform Mission Control - Epic 12 Prototype

**Phase:** 4 - Solution Discovery
**Epic:** 12 - Platform Mission Control (Admin)
**Status:** Ready for validation
**Date:** 2026-02-14

## Purpose

Validate that Sanzu platform operators (admins) can efficiently manage tenant health, diagnose issues, and remediate problems using a fleet-based operational interface.

## What This Tests

### Epic 12: Platform Mission Control

**Opportunity Hypothesis:**
Platform operability reduces support cost, improves retention, and increases trust posture.

**Target Outcomes:**
- ⏱️ Time-to-diagnosis ≤ 60 seconds
- 🔧 Time-to-remediation ≤ 2 minutes (with audit note)
- 📉 Lower support cost per tenant
- 🚨 Fewer tenant incidents

### Core Pattern: Detect → Explain → Act → Verify

1. **Detect**: Admin operational queues surface issues automatically
2. **Explain**: Event stream drilldown shows "why" with reason categories
3. **Act**: Closed-loop remediation with impact preview + audit note
4. **Verify**: System confirms resolution or re-escalates

## Features Demonstrated

### 1. Fleet Posture Overview
- At-a-glance tenant health metrics (Total, Healthy, At Risk, Critical)
- Trend indicators (improving/declining/stable)
- Real-time updates (simulated with refresh button)

### 2. Admin Operational Queues (5 Queues)
- **ADM_OnboardingStuck**: Tenants not progressing through onboarding
- **ADM_ComplianceException**: Policy breaches or retention warnings
- **ADM_KpiThresholdBreach**: Performance metrics below threshold
- **ADM_FailedPayment**: Payment issues requiring intervention
- **ADM_SupportEscalation**: High-severity incidents requiring admin action

### 3. Reason-Coded Drilldown
- Canonical reason categories (9 types): `EvidenceMissing`, `ExternalDependency`, `PolicyRestriction`, `RolePermission`, `DeadlineRisk`, `PaymentOrBilling`, `IdentityOrAuth`, `DataMismatch`, `SystemError`
- Plain-language labels for clarity
- Event stream timeline showing diagnostic history

### 4. Closed-Loop Remediation
- **Impact Preview**: Shows consequences before action is taken
- **Audit Note**: Required compliance documentation (who/why/when)
- **Verification**: System confirms action succeeded or re-escalates

### 5. Safety Guardrails
- All destructive actions require audit notes
- Impact preview prevents unintended consequences
- Role-safe with least-privilege diagnostics (design intent, not implemented in prototype)

## File Structure

```
platform-mission-control/
├── index.html              # Main dashboard UI
├── mission-control.css     # Mission control specific styles
├── mission-control.js      # Interactive behaviors
└── README.md               # This file
```

**Dependencies:**
- `../design-system/tokens/grief-aware-tokens.css` - Design tokens
- `../design-system/base.css` - Base styles and utilities
- `../design-system/components/button.css` - Button component

## How to Use the Prototype

### Open the Prototype
1. Open `index.html` in a web browser
2. Recommended: Use Chrome/Edge DevTools responsive mode to test mobile

### Key Interactions

**Queue Navigation:**
- Click queue tabs to filter by issue type
- Keyboard shortcuts: Press `1-6` to switch queues

**Investigate Issues (Detect → Explain):**
1. Click "Show details" on any queue item
2. Review reason category and event stream
3. Note recommended action
4. **Measure**: Can you understand "why" this issue exists in < 60 seconds?

**Remediate Issues (Act → Verify):**
1. Click "Remediate" button
2. Review impact preview
3. Enter audit note (required for compliance)
4. Click "Apply Remediation"
5. **Measure**: Can you complete remediation in < 2 minutes?

**Refresh Data:**
- Click refresh icon or press `R` key
- Simulates live data polling

### Keyboard Shortcuts (Power User)
- `R` - Refresh fleet metrics
- `1-6` - Switch between queue tabs
- `Escape` - Close remediation modal

## Validation Questions

### For Sanzu Admins (Internal Operators)

**Diagnosis Speed:**
1. Can you identify "tenants in trouble" within 60 seconds?
2. Does the fleet posture view help you prioritize attention?
3. Are reason categories clear and actionable?

**Remediation Efficiency:**
1. Can you remediate a standard issue within 2 minutes?
2. Does the impact preview prevent mistakes?
3. Is the audit note requirement helpful or friction?

**Operational Clarity:**
1. Do queue tabs help you focus on the right issues?
2. Is the event stream drilldown useful for diagnosis?
3. Are recommended actions appropriate?

**Safety & Compliance:**
1. Do you feel confident taking actions in this UI?
2. Does the audit trail meet compliance needs?
3. Are there missing guardrails?

## Success Criteria

### Quantitative
- [ ] ≥80% of admins can diagnose an issue in ≤60 seconds
- [ ] ≥80% of admins can remediate a standard issue in ≤2 minutes
- [ ] 0 actions taken without audit notes (100% compliance)

### Qualitative
- [ ] Admins report feeling "in control" of platform health
- [ ] Audit note requirement is seen as helpful (not just friction)
- [ ] Reason categories are clear and sufficient (no confusion)

### Go/No-Go Decision
- **Adopt**: If diagnosis/remediation time targets are met AND admins trust the safety guardrails
- **Iterate**: If targets are missed but feedback is positive on approach
- **Defer**: If admins prefer ad hoc tools over structured queues

## Known Limitations (Prototype)

This is a **non-functional prototype** for UX validation only:
- ❌ No real backend integration
- ❌ No authentication/authorization
- ❌ Simulated data only (no live updates)
- ❌ No actual remediation actions applied
- ❌ Limited to 3 queue items per queue
- ❌ No search/filter capabilities
- ❌ No bulk actions

## Next Steps

### After Validation (Week 3-4)
1. **Gather feedback** from 5+ Sanzu platform operators
2. **Measure** actual time-to-diagnosis and time-to-remediation
3. **Identify gaps** in reason categories or recommended actions
4. **Refine** based on feedback

### Production Implementation (Post-Phase 4)
1. Build backend APIs for fleet posture, queues, and remediation
2. Implement RBAC for admin actions (least-privilege)
3. Add event telemetry and reason-coded taxonomy
4. Build verification loops (auto-verify or re-escalate)
5. Integrate with support ticketing system
6. Add search, filters, and bulk actions

## Related Artifacts

**Canonical Definitions:**
- `_bmad-output/definition/mission-control-operational-queues.md` - Queue contracts
- `_bmad-output/definition/mission-control-reason-categories.md` - Reason taxonomy
- `_bmad-output/definition/mission-control-event-taxonomy.md` - Event types

**Opportunity Shaping:**
- `03-Opportunity-Shaping/opportunity-addendum-mission-control-admin-copilot-2026-02-14.md`
- `03-Opportunity-Shaping/opportunity-run-all-ideas-2026-02-14.md`

**Phase 4 Plan:**
- `04-Solution-Discovery/00_Phase_4_Kickoff_Plan.md`

## Design Decisions

### Why "Queues" Not "Dashboards"?
Dashboards are passive; queues demand action. The queue metaphor forces operational discipline and prevents issues from being ignored.

### Why Mandatory Audit Notes?
Compliance and trust. Every admin action must be explainable, especially for billing, compliance, and tenant-affecting changes.

### Why Reason Categories Instead of Free Text?
Deterministic, measurable, aggregatable. Free text blocks analytics and makes patterns invisible.

### Why Impact Preview?
Safety. Admins must see consequences before acting, especially for destructive or tenant-affecting actions.

## Feedback & Questions

For prototype feedback or questions, contact the Phase 4 validation team.

---

**Prototype Version:** 1.0
**Last Updated:** 2026-02-14
**Epic:** 12 - Platform Mission Control
