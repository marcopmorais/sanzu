# Phase 4: Solution Discovery - .NET Azure Prototype

**Goal:** Build working prototype to validate technical feasibility and core UX

---

## Week 1-2: Technical Spikes

### Spike 1: EF Core + Azure SQL (Day 1-2)
**Validate:**
- EF Core migrations with Azure SQL
- RLS-style filtering via owned entities
- Query performance with indexes

**Deliverable:** Working DbContext with test data

### Spike 2: Rules Engine (Day 3-4)
**Validate:**
- Step generation logic
- Prerequisite dependency resolution
- Status automation (Blocked → Ready)

**Test scenarios:**
- All optional workstreams
- No banks/insurance
- Complex dependencies

**Deliverable:** RulesEngine with 20 passing tests

### Spike 3: QuestPDF Generation (Day 5-6)
**Validate:**
- PDF quality (letterhead, multi-page, PT-PT chars)
- Generation speed (<5 sec)
- Template pre-fill logic

**Deliverable:** 3 professional PDF samples

### Spike 4: Azure Blob + SAS Tokens (Day 7)
**Validate:**
- Upload flow with presigned URLs
- 50MB file support
- Security (15-min expiry, RBAC checks)

**Deliverable:** Working upload/download

---

## Week 3: Prototype UI

### Option A: Blazor Server (Recommended)
**Build:**
- Case dashboard
- Questionnaire (5 questions, branching)
- Step checklist
- Document upload

**Deploy:** Azure App Service staging

### Option B: React + API Only
If Blazor learning curve too steep

---

## Week 4: Validation

### Internal Testing
- 3 complete case flows
- All 3 roles (Manager/Editor/Reader)
- Mobile + desktop

### Technical Validation
- [ ] Rules engine handles 20 scenarios
- [ ] PDF quality professional
- [ ] Upload handles 50MB files
- [ ] Performance acceptable (<2s pages)

### Go/No-Go Decision
**Proceed if:**
- All spikes successful
- No technical blockers
- Prototype completable in 60min

**Iterate if:**
- Minor UX issues
- Performance needs tuning

---

## Deliverables

1. **Working prototype:** staging-sanzu.azurewebsites.net
2. **Spike reports:** Technical validation docs
3. **Code samples:** Rules engine, PDF service
4. **Decision:** Proceed to Week 5 (full build)

---

## Success Metrics

**Technical:**
- EF Core migrations work
- Rules engine deterministic
- PDF generation <5 sec
- No security gaps

**UX:**
- Onboarding flow completable
- Next step always clear
- Mobile usable

