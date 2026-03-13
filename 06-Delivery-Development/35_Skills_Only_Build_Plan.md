# Sanzu Build Plan - Skills-Only Approach

**No agencies needed - using installed Claude skills end-to-end**

---

## PHASE 1: Foundation (Today - Day 3)

### Day 1: Database + Schema
**Skills:** `senior-backend`, `senior-data-engineer`

```
Use senior-backend skill to:
1. Review schema.sql for best practices
2. Add missing indexes for performance
3. Validate RLS policies for security
4. Generate TypeScript types from schema
```

**Deliverable:** Production-ready Supabase schema

---

### Day 2: Rules Engine
**Skills:** `senior-backend`, `senior-architect`

```
Use senior-backend to implement:
- Step library parser (from 11_Step_Library_Portugal_Phase1.md)
- Trigger evaluation (has_banks, has_insurance, etc.)
- Dependency resolver
- Status automation logic

Include comprehensive unit tests (Jest)
```

**Deliverable:** rules-engine.ts with 20 test cases passing

---

### Day 3: Template Engine
**Skills:** `senior-fullstack`, `docx`, `pdf`

```
Use pdf skill to:
1. Create React template for bank_notification_letter
2. Implement Puppeteer generator
3. Add pre-fill logic from case data
4. Handle missing fields gracefully

Test output quality with sample data
```

**Deliverable:** 3 PDF templates working

---

## PHASE 2: Frontend (Day 4-7)

### Day 4-5: Core UI
**Skills:** `frontend-design`, `ui-design-system`, `senior-frontend`

```
Use frontend-design to create:
- Case dashboard (PT-PT language)
- Questionnaire flow (progressive disclosure)
- Document upload component
- Step checklist view

Apply brand-guidelines skill for Anthropic-style colors
Use ui-design-system for component consistency
```

**Deliverable:** Production-quality React components

---

### Day 6: RBAC + Auth
**Skills:** `senior-security`, `senior-backend`

```
Use senior-security to:
1. Review RLS policies
2. Implement client-side permission checks
3. Add CSRF protection
4. Validate session management

Test all 3 roles (Manager/Editor/Reader)
```

**Deliverable:** Secure auth flows

---

### Day 7: Integration
**Skills:** `senior-fullstack`, `senior-devops`

```
Wire frontend to backend:
- API client (React Query)
- Error handling
- Loading states
- Optimistic updates

Use senior-devops to:
- Set up CI/CD (GitHub Actions)
- Configure environment variables
- Deploy to Vercel staging
```

**Deliverable:** staging.sanzu.ai working

---

## PHASE 3: Testing + Polish (Day 8-10)

### Day 8: Testing
**Skills:** `senior-qa`, `webapp-testing`, `tdd-guide`

```
Use webapp-testing to create:
- E2E tests (Playwright)
- Critical user flows
- RBAC permission tests
- Error scenario coverage

Use tdd-guide for unit test patterns
```

**Deliverable:** >80% test coverage

---

### Day 9: Security Review
**Skills:** `senior-security`, `threat-modeling-expert`, `security-compliance`

```
Use threat-modeling-expert to:
1. STRIDE analysis
2. Attack surface mapping
3. Penetration test simulation

Use security-compliance for:
- GDPR checklist
- Audit log validation
- Data retention policies
```

**Deliverable:** Security sign-off

---

### Day 10: Documentation
**Skills:** `doc-coauthoring`, `docx`, `internal-comms`

```
Use doc-coauthoring to create:
- User guide (Manager/Editor/Reader)
- API documentation
- Deployment runbook
- Incident response plan

Use docx to format professional docs
```

**Deliverable:** Complete documentation set

---

## PHASE 4: Pilot Prep (Day 11-14)

### Day 11-12: Analytics
**Skills:** `data-analyst`, `analytics-dashboard`, `metrics-tracking`

```
Use analytics-dashboard to:
- Set up PostHog events
- Create metric dashboards
- Define success KPIs

Use metrics-tracking for:
- Funnel analysis setup
- Retention cohorts
```

**Deliverable:** Analytics instrumentation

---

### Day 13: Marketing Assets
**Skills:** `content-creator`, `brand-guidelines`, `gtm-planner`

```
Use content-creator to write:
- Landing page copy (PT-PT)
- Email templates (invites, notifications)
- Pilot onboarding guide

Use gtm-planner for:
- Pilot launch plan
- Agency outreach strategy
```

**Deliverable:** Marketing materials

---

### Day 14: Final QA
**Skills:** `senior-qa`, `senior-pm`

```
Use senior-qa for:
- Final smoke tests
- Performance validation
- Mobile testing (iOS/Android)

Use senior-pm to:
- Review against PRD
- Acceptance criteria checklist
- Launch readiness assessment
```

**Deliverable:** Production launch approved

---

## VALIDATION WITHOUT AGENCIES

### Legal Validation
**Skill:** `legal-advisor`
```
Use legal-advisor to:
- Review step library for PT compliance
- Identify legal risks
- Recommend disclaimers
```

### UX Testing
**Skill:** `ux-researcher-designer`
```
Use ux-researcher-designer to:
- Heuristic evaluation
- Accessibility audit (WCAG)
- Usability checklist
```

### Financial Modeling
**Skills:** `financial-analyst`, `revenue-operations`
```
Use financial-analyst for:
- Unit economics
- Pricing sensitivity analysis
- LTV:CAC projections

Use revenue-operations for:
- Sales process design
- Customer success playbook
```

---

## OUTPUT ARTIFACTS

### Week 1 Deliverables:
1. ✅ Supabase schema (production-ready)
2. ✅ Rules engine (tested)
3. ✅ PDF templates (3 working)
4. ✅ Core UI components
5. ✅ Auth + RBAC (secure)
6. ✅ Staging deployment

### Week 2 Deliverables:
7. ✅ E2E test suite
8. ✅ Security review complete
9. ✅ Documentation set
10. ✅ Analytics instrumented
11. ✅ Marketing assets
12. ✅ Launch-ready MVP

---

## SKILLS USAGE PLAN

| Day | Primary Skills | Output |
|---|---|---|
| 1 | senior-backend, senior-data-engineer | Schema + types |
| 2 | senior-backend, senior-architect | Rules engine |
| 3 | senior-fullstack, pdf | PDF templates |
| 4-5 | frontend-design, ui-design-system | UI components |
| 6 | senior-security, senior-backend | Auth + RBAC |
| 7 | senior-fullstack, senior-devops | Integration + deploy |
| 8 | senior-qa, webapp-testing | Test suite |
| 9 | senior-security, threat-modeling-expert | Security review |
| 10 | doc-coauthoring, docx | Documentation |
| 11-12 | data-analyst, analytics-dashboard | Analytics |
| 13 | content-creator, gtm-planner | Marketing |
| 14 | senior-qa, senior-pm | Final QA |

---

## COST: $0
All skills included in Claude subscription.
No external agencies, tools, or services needed for MVP.

**Timeline: 14 days from start to launch-ready**

