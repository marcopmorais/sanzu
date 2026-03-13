# Sanzu PDLC Assessment & Improvement Plan
**Generated:** 2026-02-11  
**Scope:** Full product documentation review across 10 PDLC phases

---

## EXECUTIVE SUMMARY

**Overall Maturity:** Phase 5 (Product Definition) - Advanced  
**Readiness to Build:** HIGH (85/100)  
**Critical Gaps:** 3 high-priority, 7 medium-priority  
**Recommended Next Action:** Validate PT legal compliance → Build V1.0 MVP

### Strengths
- Exceptionally detailed product vision and versioning strategy
- Comprehensive technical architecture with clear evolution path
- Well-structured data models and API contracts
- Strong governance and risk framework

### Critical Gaps
1. **No validated user research** - Assumptions about family/agency pain points untested
2. **Missing technical stack decisions** - No ADRs or concrete tech choices
3. **Incomplete test strategy** - Security/compliance testing undefined
4. **Vague pricing model** - "€X per case" needs real numbers and validation

---

## PHASE-BY-PHASE ASSESSMENT

### PHASE 1: Vision & Strategic Intent ✅ STRONG (90%)

**Documents:**
- 00_README.md
- 01_Product_Vision.md
- 02_Grand_Vision_Agentic_Mission_Control.md
- 03_Product_Strategy_and_Versioning.md

**Strengths:**
- Clear north star: "Completed cases/month with high satisfaction"
- Well-articulated value pillars (Clarity, Execution, Coordination, Efficiency, Relief)
- Explicit product principles (non-negotiables)
- Phased evolution path (V1→V5) with clear boundaries

**Gaps:**
- [ ] **HIGH:** No competitive landscape analysis (who else solves this? why will you win?)
- [ ] **MEDIUM:** Market sizing missing (TAM/SAM/SOM for Portugal funeral agencies)
- [ ] **LOW:** No vision artifacts (one-pager, pitch deck for stakeholders)

**Improvements Needed:**

1. **Add Competitive Analysis Section** (01_Product_Vision.md)
```markdown
## Competitive Landscape

### Direct Competitors
- Manual agency processes (primary competitor = status quo)
- Generic case management tools (not domain-specific)

### Why Sanzu Wins
- Portugal-specific step library with legal accuracy
- B2B2C distribution via agencies (locked-in workflow)
- Agentic evolution path (not just a checklist tool)

### Moats
- Step library + template governance (hard to replicate)
- Agency operational lock-in (switching cost)
- Data flywheel (cycle time optimization)
```

2. **Add Market Sizing** (01_Product_Vision.md)
```markdown
## Market Opportunity (Portugal Phase 1)

**TAM:** ~100K deaths/year × €X per case = €Y annual revenue potential
**SAM:** Deaths requiring agency support (~60-70K) × €X = €Z
**SOM (Year 1):** 15 agencies × 400 cases/year × €X = €W

*Note: Sizing assumptions require validation with industry sources*
```

---

### PHASE 2: Problem Discovery ⚠️ WEAK (40%)

**Documents:**
- 06_User_Journeys_and_Service_Blueprint.md (partial)

**Strengths:**
- Clear empathy for family overwhelm and agency pressure
- Journey phases mapped (Shock → Intake → Execution → Tracking → Closure)

**Critical Gaps:**
- [ ] **HIGH:** No primary user research conducted
- [ ] **HIGH:** No JTBD (Jobs-to-be-Done) framework validation
- [ ] **HIGH:** Assumptions not prioritized by risk
- [ ] **MEDIUM:** No persona definitions with real quotes/behaviors
- [ ] **MEDIUM:** No quantified pain points (time lost, error rates, stress metrics)

**Improvements Needed:**

1. **Create Research Plan Document**
```markdown
# User Research Plan - Sanzu Phase 1

## Research Questions
1. What % of family time is lost to post-loss admin? (quantify)
2. What causes the most rework for agencies? (error analysis)
3. What is the current cycle time distribution? (baseline metric)
4. How many status calls does a typical case generate? (volume)
5. What are families' digital literacy barriers? (UX risk)

## Methods
- **Family interviews:** 15-20 recently bereaved (past 6 months)
- **Agency shadowing:** 3-5 agencies, observe 5 cases each
- **Diary study:** 10 families track admin tasks for 2 weeks
- **Quantitative survey:** 100+ families via agency partnerships

## Timeline
- Weeks 1-2: Recruit via pilot agencies
- Weeks 3-6: Interviews + shadowing
- Weeks 7-8: Analysis + synthesis
- Week 9: Validation workshop with agencies

## Success Criteria
- Validate top 5 pain points with >80% agreement
- Quantify baseline metrics (cycle time, rework %, calls/case)
- Identify 3+ "must-have" features for V1
```

2. **Build Assumption Map** (new document)
```markdown
# Assumption Map - Sanzu

## Critical Assumptions (MUST validate before V1)

| Assumption | Risk | Validation Method | Status |
|---|---|---|---|
| Families are overwhelmed by post-loss admin | HIGH | 20 interviews | ❌ Not validated |
| Agencies lose 30%+ time to status calls | HIGH | Time-tracking study | ❌ Not validated |
| Families will adopt digital tool in grief | HIGH | Prototype testing | ❌ Not validated |
| €X/case pricing acceptable to agencies | HIGH | Pricing survey | ❌ Not validated |
| Portugal step library is legally accurate | CRITICAL | Legal expert review | ❌ Not validated |

## Important Assumptions (validate during pilot)

| Assumption | Risk | Validation Method | Status |
|---|---|---|---|
| Editor role (1 person) is sufficient | MEDIUM | Pilot observation | ❌ |
| Templates reduce rework by 20%+ | MEDIUM | A/B test in pilot | ❌ |
| Mobile-first UX needed | MEDIUM | Device usage analytics | ❌ |
```

---

### PHASE 3: Opportunity Shaping ⚠️ WEAK (35%)

**Documents:**
- None (implicit in roadmap but no formal prioritization)

**Gaps:**
- [ ] **HIGH:** No RICE scoring for V1 features
- [ ] **HIGH:** No impact/effort matrix for roadmap decisions
- [ ] **MEDIUM:** No opportunity solution tree linking pain → solution
- [ ] **MEDIUM:** No "build vs buy vs partner" analysis for key capabilities

**Improvements Needed:**

1. **Create RICE Prioritization for V1 Features**
```markdown
# Feature Prioritization - V1.0 MVP

## RICE Scoring (Reach × Impact × Confidence / Effort)

| Feature | Reach | Impact | Confidence | Effort | RICE | Priority |
|---|---:|---:|---:|---:|---:|---|
| Case creation + RBAC | 100% | 3 | 100% | 5 | 60 | P0 |
| Step library + rules engine | 100% | 3 | 80% | 8 | 30 | P0 |
| Document vault + versioning | 100% | 3 | 90% | 6 | 45 | P0 |
| Template engine (3 core templates) | 100% | 3 | 70% | 6 | 35 | P0 |
| Activity feed | 80% | 2 | 80% | 3 | 43 | P1 |
| Notifications (email only) | 70% | 2 | 90% | 4 | 32 | P1 |
| Multi-language support | 10% | 1 | 50% | 8 | 0.6 | P3 |

**P0 = V1.0 | P1 = V1.1 | P2 = V2.0 | P3 = Backlog**
```

2. **Build vs Buy vs Partner Analysis**
```markdown
# Capability Sourcing Strategy

| Capability | Build | Buy | Partner | Decision | Rationale |
|---|:---:|:---:|:---:|---|---|
| Auth + RBAC | ❌ | ✅ | ❌ | **Buy** | Auth0/Clerk - standard, low differentiation |
| Document storage | ❌ | ✅ | ❌ | **Buy** | S3/GCS - commodity |
| PDF generation | ✅ | ❌ | ❌ | **Build** | Template logic is core IP |
| Step library | ✅ | ❌ | ⚠️ | **Build+Partner** | Build engine, partner for PT legal validation |
| Payment processing | ❌ | ✅ | ❌ | **Buy** | Stripe - standard |
| Bank integrations | ❌ | ❌ | ✅ | **Partner (V3)** | Requires formal partnerships |
```

---

### PHASE 4: Solution Discovery ⚠️ WEAK (30%)

**Documents:**
- None (jump from problem → definition without experimentation)

**Critical Gaps:**
- [ ] **HIGH:** No prototypes tested with users
- [ ] **HIGH:** No technical feasibility spikes
- [ ] **MEDIUM:** No A/B test plan for key hypotheses
- [ ] **MEDIUM:** No "wizard of oz" MVP for agentic features

**Improvements Needed:**

1. **Define Prototype Testing Plan**
```markdown
# Solution Validation Experiments

## Experiment 1: Figma Prototype - Family Dashboard
**Hypothesis:** Families can understand checklist progress without training  
**Method:** 10 moderated usability tests (Figma clickable prototype)  
**Success:** 8/10 complete "find next step" task without help  
**Timeline:** Week 1-2

## Experiment 2: Template Quality Test
**Hypothesis:** Auto-generated templates reduce agency review time by 50%  
**Method:** 5 agencies test 3 templates vs manual creation  
**Success:** <5 min review time, <10% error rate  
**Timeline:** Week 3-4

## Experiment 3: Rules Engine Accuracy
**Hypothesis:** Rules engine generates correct plan for 90% of cases  
**Method:** Run 20 real cases through logic, compare to agency manual plan  
**Success:** >90% step match, <5% missing critical steps  
**Timeline:** Week 5-6
```

---

### PHASE 5: Product Definition ✅ EXCELLENT (90%)

**Documents:**
- 04_PRD_Full_Scope.md
- 05_User_Roles_and_Permissions.md
- 07_Functional_Requirements.md
- 08_NonFunctional_Requirements.md
- 09_Agentic_Architecture.md
- 10_Workflow_Rules_Engine.md
- 11_Step_Library_Portugal_Phase1.md
- 12_Document_Template_Catalogue.md
- 13_Data_Model_Extended.md
- 14_API_Contract_Drafts_Extended.md
- 15_UX_Copy_Tone_and_Content_System.md
- 25_UI_Sitemap_and_Screen_Map.md

**Strengths:**
- Exceptionally detailed PRD with versioned capability map
- Clear RBAC model (Manager/Editor/Reader)
- Comprehensive data model with proper entities
- Well-thought-out agentic architecture with safety patterns
- Step library is modular and triggerable

**Gaps:**
- [ ] **HIGH:** No Architecture Decision Records (ADRs) for tech stack
- [ ] **MEDIUM:** API contracts lack error codes and rate limits
- [ ] **MEDIUM:** Data model missing indexes and query patterns
- [ ] **MEDIUM:** Step library needs legal expert validation (PT-specific)
- [ ] **LOW:** UI sitemap lacks wireframes/mockups

**Improvements Needed:**

1. **Add Architecture Decision Records**
```markdown
# ADR-001: Tech Stack Selection

**Date:** 2026-02-11  
**Status:** Proposed  
**Deciders:** [Engineering Lead, CTO]

## Context
Need to select primary tech stack for Sanzu V1.0 with constraints:
- Fast time-to-market (<3 months to pilot)
- Support for agentic evolution (V2+)
- Portugal-based team capabilities
- Budget for early-stage startup

## Decision
- **Frontend:** Next.js 14 + React + TypeScript + Tailwind
- **Backend:** Node.js + Express/Fastify + TypeScript
- **Database:** PostgreSQL 15 (primary) + Redis (cache/sessions)
- **File Storage:** AWS S3 (or Cloudflare R2)
- **Auth:** Clerk or Auth0
- **PDF Generation:** Puppeteer + custom templates
- **Deployment:** Vercel (frontend) + Railway/Render (backend)

## Rationale
- Next.js: Fast development, great DX, easy deployment
- PostgreSQL: Proven for case management + audit trails
- TypeScript: Type safety critical for rules engine
- Clerk: Fastest auth implementation, good RBAC support

## Consequences
**Positive:**
- Rapid prototyping and iteration
- Large talent pool for hiring
- Mature ecosystem for all components

**Negative:**
- Node.js may have performance limits at scale (acceptable for V1-V2)
- Vendor lock-in risk with Vercel/Clerk (mitigated by standard APIs)

## Alternatives Considered
- **Python (Django/FastAPI):** Slower frontend integration
- **Ruby on Rails:** Smaller talent pool in PT
- **Supabase full-stack:** Less control over agentic features
```

2. **Enhance API Contracts with Error Handling**
```markdown
# API Contract: POST /cases/{case_id}/documents/{doc_id}/versions

## Request
```json
{
  "filename": "death_certificate.pdf",
  "doc_type": "death_registration_proof",
  "sensitivity": "restricted",
  "content_type": "application/pdf"
}
```

## Response (201 Created)
```json
{
  "version_id": "ver_abc123",
  "upload_url": "https://s3.../signed-url",
  "upload_expires_at": "2026-02-11T12:00:00Z"
}
```

## Error Codes
| Code | Meaning | Action |
|---|---|---|
| 400 | Invalid doc_type | Check allowed doc_type values |
| 403 | Insufficient permissions | User lacks Editor/Manager role |
| 404 | Case or document not found | Verify IDs |
| 409 | Upload in progress | Wait for previous upload to complete |
| 413 | File too large | Max 50MB per file |
| 429 | Rate limit exceeded | Max 10 uploads/min per case |
| 500 | Server error | Retry with exponential backoff |

## Rate Limits
- 10 uploads/minute per case
- 100 uploads/hour per organization
```

3. **Add Data Model Query Patterns**
```markdown
# Data Model - Query Optimization

## Critical Queries (>100/sec expected)

### Q1: Get case dashboard
```sql
SELECT 
  c.*, 
  COUNT(DISTINCT wsi.step_instance_id) FILTER (WHERE wsi.status = 'Completed') as completed_steps,
  COUNT(DISTINCT wsi.step_instance_id) as total_steps,
  COUNT(DISTINCT d.doc_id) as uploaded_docs
FROM cases c
LEFT JOIN workflow_step_instances wsi ON wsi.case_id = c.case_id
LEFT JOIN documents d ON d.case_id = c.case_id
WHERE c.case_id = $1
GROUP BY c.case_id;
```

**Indexes needed:**
```sql
CREATE INDEX idx_wsi_case_status ON workflow_step_instances(case_id, status);
CREATE INDEX idx_documents_case ON documents(case_id);
```

### Q2: Multi-case agency dashboard
```sql
SELECT 
  c.case_id,
  c.deceased_full_name,
  c.status,
  c.updated_at,
  COUNT(*) FILTER (WHERE wsi.status = 'Blocked') as blocked_count,
  MIN(wsi.updated_at) FILTER (WHERE wsi.status = 'Ready') as oldest_ready_step
FROM cases c
JOIN workflow_step_instances wsi ON wsi.case_id = c.case_id
WHERE c.org_id = $1 AND c.status IN ('Active', 'Closing')
GROUP BY c.case_id
ORDER BY blocked_count DESC, oldest_ready_step ASC
LIMIT 50;
```

**Indexes needed:**
```sql
CREATE INDEX idx_cases_org_status ON cases(org_id, status, updated_at);
CREATE INDEX idx_wsi_case_status_updated ON workflow_step_instances(case_id, status, updated_at);
```
```

---

### PHASE 6: Delivery & Execution ⚠️ PARTIAL (60%)

**Documents:**
- 23_Backlog_Epics_User_Stories_and_Acceptance.md
- 24_Testing_QA_Security_and_Runbooks.md

**Strengths:**
- Clear epic structure mapped to versions
- Security runbooks defined
- Global acceptance patterns documented

**Gaps:**
- [ ] **HIGH:** No detailed user stories with acceptance criteria
- [ ] **HIGH:** No CI/CD pipeline definition
- [ ] **MEDIUM:** Testing strategy incomplete (no E2E framework chosen)
- [ ] **MEDIUM:** No deployment strategy (blue/green, canary, etc.)
- [ ] **MEDIUM:** No developer onboarding guide

**Improvements Needed:**

1. **Convert Epics to Detailed User Stories**
```markdown
# Epic 1: Auth + Invitations

## Story 1.1: Agency Manager Invites Family Editor
**As a** Funeral Agency Manager  
**I want to** invite a family member as the Editor for a case  
**So that** they can manage documents and complete steps

### Acceptance Criteria
- [ ] Manager can enter Editor email from case creation screen
- [ ] Invitation email sent within 30 seconds
- [ ] Email contains secure invite link (expires in 7 days)
- [ ] Editor can accept invite without creating password first
- [ ] Only 1 Editor allowed per case (system enforces)
- [ ] Audit event logged: `invite_sent` and `invite_accepted`

### Technical Notes
- Use Clerk invitation API or build custom token-based flow
- Store invite token with expiry in `case_participants` table
- Email template: see `tmpl_invite_email.html`

### Test Cases
1. Happy path: valid email, invite sent, Editor accepts
2. Edge: Manager tries to invite 2nd Editor (error: "Only 1 Editor allowed")
3. Edge: Invite token expired (error: "Invite expired, request new one")
4. Security: Invite token only works for specified case_id
```

2. **Define CI/CD Pipeline**
```markdown
# CI/CD Pipeline - Sanzu V1

## Pipeline Stages

### 1. Pull Request Checks (every commit)
- Lint: ESLint + Prettier
- Type check: tsc --noEmit
- Unit tests: Jest (backend) + Vitest (frontend)
- Build: Verify no build errors
- **Passing required to merge**

### 2. Staging Deployment (every merge to `main`)
- Run full test suite (unit + integration)
- Build Docker images (backend) or Next.js (frontend)
- Deploy to staging environment (Railway/Vercel preview)
- Run E2E tests (Playwright)
- **Manual QA review before production**

### 3. Production Deployment (manual trigger)
- Tag release (semantic versioning)
- Run smoke tests on staging
- Deploy with zero-downtime strategy:
  - Database migrations (if any) with rollback plan
  - Backend: blue/green deployment
  - Frontend: atomic deployment (Vercel)
- Post-deploy health checks
- Monitor error rates for 1 hour

## Tools
- **CI:** GitHub Actions
- **Testing:** Jest, Playwright, Postman/Newman (API tests)
- **Deployment:** Vercel (frontend), Railway (backend)
- **Monitoring:** Sentry (errors), Vercel Analytics, Posthog (product)
```

---

### PHASE 7: Go-To-Market & Adoption ✅ GOOD (75%)

**Documents:**
- 18_GTM_Sales_Enablement_and_Pilot_Playbook.md
- 19_Pricing_and_Packaging.md

**Strengths:**
- Clear B2B2C model via agencies
- Pilot playbook with success metrics
- Sales narrative structured well

**Gaps:**
- [ ] **HIGH:** Pricing lacks specific numbers (€X placeholder)
- [ ] **MEDIUM:** No pricing validation plan (Van Westendorp, surveys)
- [ ] **MEDIUM:** No pilot agency selection criteria
- [ ] **MEDIUM:** No content marketing plan for awareness
- [ ] **LOW:** No case studies/testimonials roadmap

**Improvements Needed:**

1. **Pricing Validation Plan**
```markdown
# Pricing Research - Sanzu

## Validation Method: Van Westendorp Price Sensitivity Meter

### Survey Questions (to 50+ agency decision-makers)
1. At what price would this seem so expensive you would not consider it?
2. At what price would it start to seem expensive, but you'd still consider it?
3. At what price would it seem like a good value?
4. At what price would it seem too cheap to be good quality?

### Hypothesis
- **Optimal Price Point (OPP):** €45-€65 per case
- **Range of Acceptable Prices:** €30-€90 per case

### Contingency Pricing Models
**Model A: Per-case + volume tiers**
- 1-50 cases/month: €60/case
- 51-200 cases/month: €50/case
- 200+ cases/month: €40/case

**Model B: Subscription + usage**
- Base: €300/month (includes 10 cases)
- Additional: €35/case

**Model C: Freemium**
- Free: 5 cases/month
- Pro: €500/month unlimited cases

### Testing Plan
- Week 1-2: Survey 50 agencies (target: 30 responses)
- Week 3: Analyze price sensitivity curves
- Week 4: Pilot with 3-5 agencies using Model A
- Week 5-8: Collect willingness-to-pay data from pilot
- Week 9: Finalize pricing for launch
```

2. **Pilot Agency Selection Criteria**
```markdown
# Pilot Agency Selection - Criteria

## Must-Have Criteria
- [ ] 100+ cases/year (sufficient volume for data)
- [ ] Willing to dedicate 1-2 Managers for training
- [ ] Digital-forward culture (uses some software already)
- [ ] Based in Lisbon/Porto area (for in-person support)
- [ ] Committed to 60-day pilot with weekly check-ins

## Nice-to-Have Criteria
- [ ] Diverse case types (urban/rural, simple/complex)
- [ ] Existing family communication challenges (high pain)
- [ ] Referral potential (well-connected in industry)

## Disqualifiers
- Agencies with <50 cases/year (insufficient learning)
- Agencies in active M&A or leadership transition
- Agencies unwilling to share anonymized metrics

## Target: 10-15 pilot agencies
```

---

### PHASE 8: Measurement & Outcomes ✅ STRONG (85%)

**Documents:**
- 16_Analytics_KPIs_and_Event_Taxonomy_Extended.md
- 17_Roadmap_Releases_and_OKRs.md

**Strengths:**
- Clear North Star metric
- Comprehensive KPI tree
- Event taxonomy well-structured
- OKRs are measurable

**Gaps:**
- [ ] **MEDIUM:** No analytics instrumentation plan (which tool, where, when)
- [ ] **MEDIUM:** No baseline data collection strategy (pre-Sanzu metrics)
- [ ] **LOW:** No dashboards mocked up for team visibility

**Improvements Needed:**

1. **Analytics Implementation Plan**
```markdown
# Analytics Instrumentation - V1.0

## Tool Stack
- **Product Analytics:** PostHog (open-source, GDPR-friendly)
- **Error Tracking:** Sentry
- **Performance:** Vercel Analytics + Web Vitals
- **Backend Logs:** Structured logging (Winston/Pino) → CloudWatch/Datadog

## Event Tracking - Priority
| Event | When | Properties | Priority |
|---|---|---|---|
| `case_created` | Case creation | org_id, created_by_role | P0 |
| `step_status_changed` | Step update | case_id, step_key, from_status, to_status | P0 |
| `document_uploaded` | Doc upload | doc_type, sensitivity, size_bytes | P0 |
| `template_generated` | Template gen | template_id, generation_time_ms | P0 |
| `case_closed` | Case closure | cycle_time_hours, completion_rate | P0 |
| `nps_submitted` | NPS survey | score, role, case_id | P1 |

## Dashboard Requirements
**Agency Manager View:**
- Active cases count
- Avg cycle time (this month vs last month)
- Blocked steps count + age distribution
- Template generation success rate

**Admin/Product View:**
- WAU/MAU (agencies + families)
- Funnel: invite_sent → invite_accepted → questionnaire_completed → case_closed
- Time-to-next-action distribution (P50, P90, P95)
- Error rates by endpoint
```

---

### PHASE 9: Scaling & Evolution ⚠️ EARLY (40%)

**Documents:**
- None explicit (covered in versioning strategy)

**Gaps:**
- [ ] **MEDIUM:** No team scaling plan (when to hire what roles)
- [ ] **MEDIUM:** No infrastructure scaling strategy (DB sharding, etc.)
- [ ] **MEDIUM:** No internationalization plan (beyond Portugal)
- [ ] **LOW:** No partner ecosystem development plan

**Improvements Needed:**

1. **Team Scaling Roadmap**
```markdown
# Team Hiring Plan - Sanzu

## Phase 1: MVP Team (Now → V1.0)
- [ ] 1 Full-stack Engineer (founding team)
- [ ] 1 Product Manager (you)
- [ ] 1 PT Legal/Compliance Advisor (contract)
- [ ] 1 Designer (contract, 20h/week)

## Phase 2: Pilot → V1.2 (6 months)
- [ ] +1 Backend Engineer (focus: rules engine, integrations)
- [ ] +1 Frontend Engineer (focus: UX polish, mobile)
- [ ] +1 Customer Success Manager (agency onboarding)

## Phase 3: V2.0 → Scale (12 months)
- [ ] +1 Data Engineer (analytics, reporting)
- [ ] +1 DevOps/SRE (reliability, security)
- [ ] +2 Engineers (features + maintenance)
- [ ] +1 Sales (agency acquisition)

## Total by End of Year 1: 9-10 people
```

---

### PHASE 10: Governance & Renewal ⚠️ PARTIAL (55%)

**Documents:**
- 21_Compliance_Privacy_Retention_and_DPA_Notes.md
- 22_Risks_Governance_and_Operating_Model.md

**Strengths:**
- Risk matrix with mitigations
- Governance cadence defined
- Compliance baseline (GDPR) outlined

**Gaps:**
- [ ] **HIGH:** No privacy impact assessment (PIA) for sensitive data
- [ ] **MEDIUM:** DPA template needs legal review (PT-specific)
- [ ] **MEDIUM:** No incident response runbook tested
- [ ] **LOW:** No product sunset criteria (if pivot needed)

**Improvements Needed:**

1. **Privacy Impact Assessment (PIA)**
```markdown
# Privacy Impact Assessment - Sanzu

## Data Processing Overview
**Personal Data Collected:**
- Family: name, email, phone, tax ID (NIF), relationship to deceased
- Deceased: name, date of death, tax ID, municipality
- Documents: ID scans, death certificates, bank statements

**Lawful Basis:**
- Legitimate interest (post-loss admin is necessary)
- Consent (for family participation in digital process)

## Risks & Mitigations
| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Unauthorized access to sensitive docs | HIGH | LOW | RBAC + encryption + audit logs |
| Data breach via agency account compromise | HIGH | MEDIUM | MFA mandatory for agencies |
| Family unaware of data usage | MEDIUM | MEDIUM | Clear privacy notice at invite |
| Data retained too long | MEDIUM | LOW | Configurable retention + auto-delete |

## Data Subject Rights
- **Access:** Export case data via UI (Manager/Editor)
- **Rectification:** Edit case data via UI
- **Deletion:** Request via email → manual review → delete within 30 days
- **Portability:** JSON export of full case data

## DPO Contact (if required)
- TBD: Assess if Sanzu needs Data Protection Officer (depends on scale)
```

---

## CONSOLIDATED IMPROVEMENT ROADMAP

### Immediate (Before V1 Development)
1. ✅ **Validate PT legal compliance** for step library (contract expert)
2. ✅ **Conduct 10-15 family interviews** to validate pain points
3. ✅ **Create ADRs for tech stack** (see template above)
4. ✅ **Finalize pricing** via Van Westendorp survey
5. ✅ **Prototype test** family dashboard (Figma → 10 users)

### During V1 Development
6. ✅ **Write detailed user stories** with acceptance criteria (backlog)
7. ✅ **Set up CI/CD pipeline** (GitHub Actions + Vercel/Railway)
8. ✅ **Instrument analytics** (PostHog events for P0 metrics)
9. ✅ **Build pilot playbook** with agency selection criteria
10. ✅ **Draft DPA template** for agency contracts

### Post-V1 Launch
11. ✅ **Baseline metrics** from pilot (cycle time, rework, NPS)
12. ✅ **Iterate based on pilot learnings** (V1.1 roadmap)
13. ✅ **Expand pilot** to 15+ agencies
14. ✅ **Prepare V2 roadmap** (agency ops dashboard)

---

## NEXT ACTIONS (THIS WEEK)

**Priority 1: De-risk legal compliance**
- [ ] Contract PT legal expert for step library review
- [ ] Identify top 3 compliance gaps
- [ ] Create validation checklist

**Priority 2: Validate core assumptions**
- [ ] Recruit 10 families for interviews (via agency contacts)
- [ ] Prepare interview guide (see Phase 2 template)
- [ ] Schedule interviews

**Priority 3: Tech foundation**
- [ ] Write ADR-001 (tech stack selection)
- [ ] Set up GitHub repo + CI/CD skeleton
- [ ] Spike: PDF generation library test (Puppeteer vs alternatives)

**Priority 4: Pilot prep**
- [ ] Draft pilot agency outreach email
- [ ] Create 1-page pilot proposal
- [ ] Identify 20-30 target agencies in Lisbon/Porto

---

## METRICS TO TRACK (Week 0)

| Metric | Target | Actual | Status |
|---|---|---|---|
| Legal review completed | Yes | ❌ | Not started |
| Family interviews scheduled | 10 | 0 | Not started |
| ADRs written | 3 | 0 | Not started |
| Pilot agencies contacted | 20 | 0 | Not started |
| Pricing survey responses | 30 | 0 | Not started |

---

**Assessment Confidence:** HIGH  
**Ready to Build:** YES (after legal validation)  
**Estimated Time to V1.0 MVP:** 10-12 weeks with 2-3 engineers

