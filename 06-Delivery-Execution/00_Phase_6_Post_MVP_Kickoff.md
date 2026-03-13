# Phase 6: Delivery & Execution - Post-MVP Implementation Plan

**Date:** 2026-02-14
**Phase:** 6 - Delivery & Execution (Post-MVP)
**Scope:** Epics 9-13 Implementation
**Status:** Ready to Execute

---

## Executive Summary

Phase 4 (Solution Discovery) validated the feasibility and desirability of Post-MVP features through:
- ✅ Production-ready grief-aware design system (10 components, WCAG 2.1 AA)
- ✅ Family Dashboard prototype (Epic 9 - Plain-language glossary + Blocked recovery)
- ✅ Platform Mission Control prototype (Epic 12 - Fleet + Queues + Remediation)

**Phase 6 Objective:** Implement Epics 9-13 using validated design patterns and prototypes, integrating seamlessly with MVP foundation (Epics 1-8).

---

## Post-MVP Epic Summary

### Epic 9: Guided Experience, Plain Language, and Recovery ⭐ HIGH PRIORITY
**Value:** Reduce family confusion and blocked-step resolution time
**Validated:** Family Dashboard prototype ready for implementation
**Stories:**
- 9.1: Plain-Language Glossary and Contextual Explanations (FR64, NFR13)
- 9.2: Reason-Coded Blocked States and Guided Recovery (FR65, NFR5, NFR13)

**Dependencies:** Epic 3 (Workflow), Epic 4 (Documents)
**Prototype Assets:** `04-Solution-Discovery/prototypes/family-dashboard/`

---

### Epic 10: Agency Playbooks and Governed Defaults
**Value:** Consistent case handling, reduced training overhead
**Stories:**
- 10.1: Define and Version Agency Playbooks (FR66, NFR6, NFR23)
- 10.2: Apply Playbooks to New Cases (FR66, NFR6)

**Dependencies:** Epic 1 (Onboarding), Epic 2 (Case Lifecycle)
**Technical Needs:** Playbook versioning system, default application engine

---

### Epic 11: Compliance Export and Trust Telemetry
**Value:** Audit readiness, operational intelligence
**Stories:**
- 11.1: Export Case Audit and Evidence Package (FR67, NFR6, NFR23)
- 11.2: Trust Telemetry for Pilot Learning (FR68, NFR12, NFR25)

**Dependencies:** Epic 4 (Documents), Epic 6 (Governance), Epic 7 (Admin)
**Technical Needs:** Export pipeline, telemetry aggregation, redaction engine

---

### Epic 12: Platform Mission Control ⭐ HIGH PRIORITY
**Value:** Admin operational efficiency, faster incident resolution
**Validated:** Platform Mission Control prototype ready for implementation
**Stories:**
- 12.1: Tenant Fleet Posture View (FR69, NFR1, NFR6, NFR12, NFR22)
- 12.2: Admin Queues with Event Stream (FR70, FR71, NFR1, NFR6)
- 12.3: Closed-Loop Remediation (FR72, NFR6, NFR12, NFR22, NFR25)

**Dependencies:** Epic 7 (Platform Admin)
**Prototype Assets:** `04-Solution-Discovery/prototypes/platform-mission-control/`

---

### Epic 13: Agentic Copilot (Drafting-First)
**Value:** Reduced coordination overhead, faster recovery
**Stories:**
- 13.1: Draft Evidence Requests and Handoff Checklists (FR73, FR74, NFR5, NFR6)
- 13.2: Draft Recovery Plans with "Why" Explanations (FR73, FR74, NFR5, NFR6)

**Dependencies:** Epic 3 (Workflow), Epic 5 (Handoffs), Epic 9 (Guided), Epic 12 (Mission Control)
**Technical Needs:** LLM integration, drafting templates, role-safety guardrails

---

## Implementation Strategy

### Phase 1: Foundation (Weeks 1-2)
**Objective:** Integrate design system and prepare infrastructure

#### Sprint 1.1: Design System Integration
- **Task 1.1.1:** Integrate design system into Next.js application
  - Copy `design-system/` to `api/src/Sanzu.Web/public/design-system/`
  - Update layout templates to import design tokens
  - Create React component wrappers for Button, Input, Card, Badge, Modal, Alert
  - Test component rendering and responsiveness

- **Task 1.1.2:** Update existing MVP components to use design system
  - Migrate button styles to `.btn` classes
  - Migrate form inputs to `.input` classes
  - Apply grief-aware color tokens to existing UI
  - Verify WCAG 2.1 AA compliance across all pages

#### Sprint 1.2: Infrastructure Preparation
- **Task 1.2.1:** Create reason-category taxonomy tables
  - Implement `mission-control-reason-categories.md` schema
  - Seed canonical reason categories (9 types)
  - Create API endpoints for reason-coded events

- **Task 1.2.2:** Set up telemetry pipeline
  - Configure event tracking for workflow state changes
  - Implement reason-coded event logging
  - Create aggregation queries for admin dashboards

---

### Phase 2: Epic 9 Implementation (Weeks 3-4) ⭐ PRIORITY
**Objective:** Implement Guided Experience features

#### Sprint 2.1: Plain-Language Glossary (Story 9.1)
- **Task 2.1.1:** Create glossary content management
  - Define glossary term schema (term, plain_language_definition, why_it_matters, role_visibility)
  - Create CRUD APIs for glossary terms
  - Build admin UI for glossary management

- **Task 2.1.2:** Integrate glossary tooltips into family UI
  - Use `tooltip-glossary` component from design system
  - Add glossary triggers to legal terms throughout UI
  - Implement keyboard navigation and screen reader support
  - **Reference:** `04-Solution-Discovery/prototypes/family-dashboard/index.html` lines 74-93

- **Task 2.1.3:** Test with bereaved families
  - Usability testing with 5-10 participants
  - Measure: comprehension improvement, confidence increase
  - Iterate based on feedback

#### Sprint 2.2: Reason-Coded Blocked Recovery (Story 9.2)
- **Task 2.2.1:** Implement blocked state detection
  - Create workflow state analyzer to detect blocked conditions
  - Map blocked states to canonical reason categories
  - Store blocked reason in workflow state

- **Task 2.2.2:** Build recovery action engine
  - Define recovery action templates per reason category
  - Implement role-safe recovery action filtering
  - Create recovery action execution endpoints

- **Task 2.2.3:** Integrate blocked recovery UI
  - Use `.card-blocked` and `.recovery-action` components
  - Display reason-coded explanation
  - Show guided recovery steps
  - **Reference:** `04-Solution-Discovery/prototypes/family-dashboard/index.html` lines 108-168

**Definition of Done:**
- [ ] Glossary terms manageable via admin UI
- [ ] Tooltips appear on all legal terms in family dashboard
- [ ] Blocked steps show reason-coded explanations
- [ ] Recovery actions execute without errors
- [ ] WCAG 2.1 AA compliance verified
- [ ] User testing shows 20%+ comprehension improvement

---

### Phase 3: Epic 12 Implementation (Weeks 5-7) ⭐ PRIORITY
**Objective:** Implement Platform Mission Control

#### Sprint 3.1: Fleet Posture View (Story 12.1)
- **Task 3.1.1:** Build tenant health aggregation
  - Create background job to compute tenant health scores
  - Aggregate KPIs (case completion rate, blocked rate, overdue count)
  - Store fleet posture in cache for fast retrieval

- **Task 3.1.2:** Build fleet dashboard UI
  - Use metric cards from design system
  - Display: Total, Healthy, At Risk, Critical tenants
  - Add trend indicators (↗ ↘ →)
  - **Reference:** `04-Solution-Discovery/prototypes/platform-mission-control/index.html` lines 56-89

#### Sprint 3.2: Admin Operational Queues (Story 12.2)
- **Task 3.2.1:** Implement queue detection logic
  - Create queue scanners for 5 admin queues:
    - `ADM_OnboardingStuck`
    - `ADM_ComplianceException`
    - `ADM_KpiThresholdBreach`
    - `ADM_FailedPayment`
    - `ADM_SupportEscalation`
  - Store queue items with reason categories

- **Task 3.2.2:** Build queue UI with tabs
  - Use `tabs` component for queue navigation
  - Display queue items with severity indicators
  - Implement expandable details panels
  - **Reference:** `04-Solution-Discovery/prototypes/platform-mission-control/index.html` lines 99-268

- **Task 3.2.3:** Build event stream drilldown
  - Display chronological event timeline
  - Show reason categories and event types
  - Implement filtering and search

#### Sprint 3.3: Closed-Loop Remediation (Story 12.3)
- **Task 3.3.1:** Build remediation workflow
  - Implement impact preview engine
  - Create audit note capture form
  - Build verification tracking

- **Task 3.3.2:** Build remediation modal UI
  - Use `modal` component with confirmation variant
  - Display impact preview panel
  - Require audit note (compliance)
  - Show verification outcome
  - **Reference:** `04-Solution-Discovery/prototypes/platform-mission-control/index.html` lines 271-308

**Definition of Done:**
- [ ] Fleet posture shows real-time tenant health
- [ ] 5 admin queues populate automatically
- [ ] Queue items show reason-coded drilldown
- [ ] Remediation actions require audit notes
- [ ] Time-to-diagnosis ≤60s, time-to-remediation ≤2m
- [ ] All actions are auditable and reversible

---

### Phase 4: Epic 10 & 11 Implementation (Weeks 8-10)
**Objective:** Implement Playbooks and Compliance

#### Sprint 4.1: Agency Playbooks (Epic 10)
- **Task 4.1.1:** Build playbook management (Story 10.1)
  - Create playbook schema (templates, defaults, rules)
  - Implement versioning system
  - Build admin UI for playbook CRUD

- **Task 4.1.2:** Implement playbook application (Story 10.2)
  - Apply playbook defaults on case creation
  - Record applied playbook version in timeline
  - Test playbook inheritance

#### Sprint 4.2: Compliance Export (Story 11.1)
- **Task 4.2.1:** Build export pipeline
  - Aggregate audit events, evidence, status history
  - Implement redaction rules (role-based)
  - Generate PDF/ZIP export packages

#### Sprint 4.3: Trust Telemetry (Story 11.2)
- **Task 4.3.1:** Build telemetry aggregation
  - Aggregate reason-coded counts
  - Calculate operational metrics (time-to-first-action, blocked-resolution time)
  - Create telemetry API with filtering

- **Task 4.3.2:** Build telemetry dashboard
  - Use `table` component for telemetry data
  - Add filtering by tenant, cohort, time period
  - Display trend charts

**Definition of Done:**
- [ ] Playbooks apply to new cases automatically
- [ ] Export includes audit trail + evidence
- [ ] Telemetry shows operational patterns
- [ ] All exports are redacted per role

---

### Phase 5: Epic 13 Implementation (Weeks 11-12)
**Objective:** Implement Agentic Copilot (Drafting-First)

#### Sprint 5.1: Evidence Request Drafts (Story 13.1)
- **Task 5.1.1:** Build drafting engine
  - Integrate LLM for draft generation
  - Create evidence request templates
  - Implement role-safety filters

- **Task 5.1.2:** Build draft UI
  - Show draft preview
  - Allow user edit before sending
  - Require explicit confirmation

#### Sprint 5.2: Recovery Plan Drafts (Story 13.2)
- **Task 5.2.1:** Build explainability engine
  - Generate "why" explanations from reason categories
  - Draft recovery plans with step-by-step guidance
  - Include "based on" evidence links

**Definition of Done:**
- [ ] Copilot generates role-safe drafts
- [ ] Users must confirm before sending
- [ ] Explanations reference reason categories
- [ ] No autonomous actions without confirmation

---

## Technical Architecture

### Design System Integration
```
api/src/Sanzu.Web/
├── public/design-system/              # Copy from 04-Solution-Discovery/prototypes/
│   ├── tokens/grief-aware-tokens.css
│   ├── base.css
│   └── components/*.css
├── app/components/                    # React wrappers
│   ├── Button.tsx
│   ├── Input.tsx
│   ├── Card.tsx
│   ├── Badge.tsx
│   ├── Modal.tsx
│   ├── Alert.tsx
│   ├── Tabs.tsx
│   ├── Dropdown.tsx
│   ├── Tooltip.tsx
│   └── Table.tsx
└── app/layout.tsx                     # Import design system CSS
```

### Database Schema Extensions
```sql
-- Glossary Terms (Epic 9.1)
CREATE TABLE glossary_terms (
  id UUID PRIMARY KEY,
  term VARCHAR(255) NOT NULL,
  plain_language_definition TEXT NOT NULL,
  why_it_matters TEXT,
  role_visibility VARCHAR(50)[], -- ['family', 'agency', 'admin']
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

-- Reason-Coded Events (Epic 9.2, 12.2)
CREATE TABLE workflow_events (
  id UUID PRIMARY KEY,
  case_id UUID REFERENCES cases(id),
  event_type VARCHAR(100) NOT NULL,
  reason_category VARCHAR(50), -- EvidenceMissing, ExternalDependency, etc.
  reason_detail TEXT,
  actor_id UUID,
  occurred_at TIMESTAMP DEFAULT NOW()
);

-- Admin Queue Items (Epic 12.2)
CREATE TABLE admin_queue_items (
  id UUID PRIMARY KEY,
  queue_id VARCHAR(50) NOT NULL, -- ADM_OnboardingStuck, ADM_KpiThresholdBreach, etc.
  tenant_id UUID REFERENCES tenants(id),
  severity VARCHAR(20), -- critical, warning, info
  reason_category VARCHAR(50),
  detected_at TIMESTAMP DEFAULT NOW(),
  resolved_at TIMESTAMP,
  resolution_note TEXT
);

-- Playbooks (Epic 10.1)
CREATE TABLE agency_playbooks (
  id UUID PRIMARY KEY,
  tenant_id UUID REFERENCES tenants(id),
  version INT NOT NULL,
  template_defaults JSONB,
  workflow_rules JSONB,
  created_at TIMESTAMP DEFAULT NOW(),
  is_active BOOLEAN DEFAULT FALSE
);
```

### API Endpoints

**Epic 9: Guided Experience**
- `GET /api/glossary/terms` - List glossary terms
- `GET /api/glossary/terms/:id` - Get term definition
- `POST /api/admin/glossary/terms` - Create term (admin)
- `GET /api/cases/:id/blocked-steps` - Get blocked steps with reason codes
- `POST /api/cases/:id/recovery-actions/:actionId` - Execute recovery action

**Epic 12: Platform Mission Control**
- `GET /api/admin/fleet/posture` - Get tenant health overview
- `GET /api/admin/queues` - List all queue items
- `GET /api/admin/queues/:queueId` - Get queue items for specific queue
- `GET /api/admin/events` - Get event stream with filters
- `POST /api/admin/remediate` - Execute remediation action (with audit note)

---

## Success Criteria

### Epic 9: Guided Experience
- [ ] Glossary tooltips on all legal terms (family dashboard)
- [ ] Blocked steps show reason-coded explanations + recovery actions
- [ ] User testing: 20%+ comprehension improvement, 70%+ task completion
- [ ] WCAG 2.1 AA compliance verified

### Epic 12: Platform Mission Control
- [ ] Fleet posture shows real-time tenant health (4 metrics)
- [ ] 5 admin queues populate automatically based on reason categories
- [ ] Time-to-diagnosis ≤ 60 seconds
- [ ] Time-to-remediation ≤ 2 minutes (with audit note)
- [ ] 100% of admin actions are auditable

### Epic 10: Agency Playbooks
- [ ] Playbooks apply to new cases automatically
- [ ] Playbook versions are retained for auditability

### Epic 11: Compliance Export
- [ ] Exports include audit trail + evidence (redacted per role)
- [ ] Telemetry shows operational patterns (reason-coded counts, metrics)

### Epic 13: Agentic Copilot
- [ ] Copilot generates role-safe drafts (evidence requests, recovery plans)
- [ ] Users must confirm before sending (no autonomous actions)
- [ ] Explanations reference reason categories and events

---

## Risk Mitigation

### Risk 1: Design System Integration Complexity
**Mitigation:** Start with React component wrappers in Sprint 1.1. Use existing prototypes as reference implementation.

### Risk 2: Reason-Category Taxonomy Evolution
**Mitigation:** Store reason categories as enum values with versioning. Allow new categories without breaking existing events.

### Risk 3: LLM Integration Latency (Epic 13)
**Mitigation:** Implement async draft generation with loading states. Cache common draft templates.

### Risk 4: Admin Action Safety (Epic 12.3)
**Mitigation:** Require impact preview + audit note for all actions. Implement action verification tracking.

---

## Timeline

**Total Duration:** 12 weeks (3 months)

| Phase | Weeks | Focus | Deliverables |
|-------|-------|-------|--------------|
| Phase 1 | 1-2 | Foundation | Design system integrated, infrastructure ready |
| Phase 2 | 3-4 | Epic 9 | Glossary + Blocked recovery live |
| Phase 3 | 5-7 | Epic 12 | Platform Mission Control operational |
| Phase 4 | 8-10 | Epics 10-11 | Playbooks + Compliance + Telemetry |
| Phase 5 | 11-12 | Epic 13 | Agentic Copilot (drafting-first) |

**Milestone Dates:**
- Week 2: Design system integrated ✅
- Week 4: Epic 9 deployed to staging
- Week 7: Epic 12 deployed to staging
- Week 10: Epics 10-11 deployed to staging
- Week 12: Epic 13 deployed to staging, Post-MVP complete 🎉

---

## Resources

### Design Assets
- Design System: `04-Solution-Discovery/prototypes/design-system/`
- Family Dashboard Prototype: `04-Solution-Discovery/prototypes/family-dashboard/`
- Platform Mission Control Prototype: `04-Solution-Discovery/prototypes/platform-mission-control/`

### Documentation
- PRD: `_bmad-output/definition/prd.md`
- Epics & Stories: `_bmad-output/definition/epics_and_stories.md`
- Reason Categories: `_bmad-output/definition/mission-control-reason-categories.md`
- Operational Queues: `_bmad-output/definition/mission-control-operational-queues.md`
- Event Taxonomy: `_bmad-output/definition/mission-control-event-taxonomy.md`

### Sprint Tracking
- Sprint Status: `_bmad-output/implementation-artifacts/sprint-status.yaml`

---

## Next Immediate Actions

1. **Create Sprint 1.1 backlog** (Design System Integration)
2. **Set up development environment** for Post-MVP work
3. **Create React component wrappers** for design system
4. **Begin reason-category taxonomy implementation**

---

**Phase Status:** Ready to Execute
**Confidence Level:** HIGH (validated prototypes, production-ready design system)
**Blocker Status:** None

Let's build Post-MVP! 🚀
