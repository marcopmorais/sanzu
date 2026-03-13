# 10-Week Implementation Roadmap (No Research)

**Approach:** Build → Pilot → Learn  
**Team:** 1 Technical PM + AI tools  
**Timeline:** Feb 11 → Apr 22, 2026

---

## WEEK 1-2: Foundation (Feb 11-24)

### Week 1: Setup
**Day 1-2:** Legal expert contracting
- Send RFP to 3 PT legal firms
- Contract selected expert (€2.5k)
- Share step library for review

**Day 3-5:** Technical setup
- Supabase project creation
- GitHub repo + monorepo structure
- Vercel deployment pipeline
- Environment configs

**Deliverable:** Legal review started, infra ready

### Week 2: Validation Spikes
**Day 1:** PDF quality spike (Puppeteer test)
**Day 2-3:** Rules engine spike (step generation)
**Day 4:** RLS performance spike (query tests)
**Day 5:** Legal review complete + fixes

**Deliverable:** Technical feasibility confirmed, legal validated

---

## WEEK 3-4: Rapid Prototype (Feb 25 - Mar 10)

### Week 3: Core Flows
```
v0: Family onboarding + questionnaire
v0: Manager dashboard + case creation
Claude Code: Supabase schema + RLS
```

**Deliverable:** staging.sanzu.ai working

### Week 4: Validation
- Deploy to staging
- 3 agencies test internally
- Feedback collection (Typeform)
- Iterate based on feedback

**Deliverable:** Go/no-go decision by Mar 10

---

## WEEK 5-10: V1 Development (Mar 11 - Apr 22)

### Week 5: Sprint 1 - Auth & Cases
**AI generates:**
- Supabase auth + invite flow
- Case CRUD with RLS
- Audit logging

**Human validates:** RBAC, permissions

### Week 6-7: Sprint 2 - Workflow
**AI generates:**
- Rules engine implementation
- Questionnaire with branching
- Step generation + dependencies

**Human validates:** 20 test scenarios

### Week 8: Sprint 3 - Documents
**AI generates:**
- S3 upload with presigned URLs
- Document vault UI
- 3 PDF templates (Puppeteer)

**Human validates:** PDF quality, mobile upload

### Week 9: Sprint 4 - Polish
**AI generates:**
- Dashboard with progress
- Activity feed
- Email notifications (Resend)

**Human validates:** E2E flows

### Week 10: QA & Deploy
- Security review
- E2E test suite
- Performance testing
- Production deployment

**Deliverable:** V1.0 production-ready

---

## WEEK 11-14: Pilot (Apr 22 - May 20)

### Pilot Plan
- Onboard 5 agencies
- Run 25+ cases with support
- Weekly metrics review
- Iterate based on feedback

**Success metrics:**
- >80% case completion
- <14 days median cycle time
- NPS ≥40 (family + agency)

---

## RESOURCE ALLOCATION

| Week | Focus | Time Commitment |
|---|---|---|
| 1-2 | Setup + Legal | 20h/week |
| 3-4 | Prototype | 30h/week |
| 5-10 | Development | 40h/week |
| 11-14 | Pilot | 30h/week |

**Total:** ~360 hours over 14 weeks

---

## BUDGET (10-Week Build)

| Item | Cost |
|---|---|
| Legal expert | €2,500 |
| Technical PM (2.5 months) | €15,000 |
| AI tools (3 months) | €180 |
| Infrastructure | €300 |
| Pilot support | €3,000 |
| **Total** | **€21,000** |

---

## MILESTONES

**Feb 24:** Legal validated, spikes done  
**Mar 10:** Prototype validated, GO decision  
**Apr 22:** V1.0 production launch  
**May 20:** Pilot complete, metrics reviewed

---

## DECISION GATES

### Gate 1 (Week 2): Technical Feasibility
- PDF spike passes
- Rules engine spike passes
- RLS performance acceptable
- **Decision:** Proceed to prototype

### Gate 2 (Week 4): Product Validation
- 3 agencies test staging
- Average rating ≥7/10
- <3 critical UX issues
- **Decision:** Proceed to V1 build

### Gate 3 (Week 10): Production Readiness
- Security review passes
- E2E tests passing
- Performance acceptable
- **Decision:** Launch to pilot

### Gate 4 (Week 14): Scale Decision
- Pilot metrics hit targets
- Agencies willing to pay
- **Decision:** Scale or iterate

