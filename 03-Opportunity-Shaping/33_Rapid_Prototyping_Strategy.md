# Rapid Prototyping Strategy - AI-First Approach

**Phase:** Solution Discovery (Weeks 3-4)  
**Goal:** Validate core UX without traditional research

---

## STRATEGY: Build to Learn

Instead of Figma → Test → Build, we go straight to:
**Build with AI → Deploy → Learn from pilot**

### Why This Works
- v0 generates production-quality UI
- Supabase = instant backend
- Deploy to staging in hours
- Iterate based on pilot feedback

---

## WEEK 3-4 DELIVERABLES

### 1. Interactive Prototype (v0 + Vercel)

**Core Flows to Build:**
```
v0: "Family onboarding flow PT-PT:
1. Accept invite email
2. Passwordless login
3. Welcome screen (case context)
4. Questionnaire (5 questions, branching)
5. Dashboard (next step, progress)"
```

**Output:** Working app on staging.sanzu.ai  
**Test with:** 2-3 pilot agencies (internal testing)

### 2. Template Previews

```
Claude Code: "Generate 3 PDF templates:
- Bank notification letter
- Insurance claim request  
- Service cancellation

Pre-fill with sample data, output to /examples folder"
```

**Output:** PDF samples to show agencies  
**Validate:** Professional appearance, correct PT-PT

### 3. Manager Dashboard

```
v0: "Agency manager dashboard:
- Case list (filters: status, date)
- Quick actions (create case, invite)
- Stats (active cases, completion rate)
Mobile + desktop responsive"
```

**Output:** Demo-ready interface  
**Test with:** Agency managers (pilot candidates)

---

## VALIDATION APPROACH

### Skip Usability Testing
**Traditional:** 10 users × 60 min = 10 hours + recruitment  
**AI-First:** Deploy → 3 agencies self-test → feedback form

### Feedback Collection
```
Simple Typeform after pilot agencies try staging:
1. Could you complete [task]? (yes/no)
2. What was confusing? (open text)
3. What's missing? (open text)
4. Would you use this? (1-10 scale)
```

### Iteration Speed
- Feedback → AI prompt → Deploy: **2-4 hours**
- Traditional: Feedback → Design → Code → Deploy: **2-4 days**

---

## TECHNICAL FEASIBILITY VALIDATION

### Spike 1: Puppeteer PDF Quality
```bash
# Test template rendering
Claude Code: "Generate PDF from React template.
Test: multi-page, tables, Portuguese characters, letterhead"
```

**Success criteria:** Professional output, <10 sec generation  
**Timeline:** Day 1

### Spike 2: Rules Engine Complexity
```bash
# Test step generation logic
Claude Code: "Implement step trigger logic.
Test case: has_banks=true → generates 3 bank steps with dependencies"
```

**Success criteria:** Correct steps generated, status auto-updates  
**Timeline:** Day 2-3

### Spike 3: Supabase RLS Performance
```sql
-- Test row-level security at scale
INSERT 1000 test cases, 10 users, verify:
- Managers see only org cases
- Editors see only assigned
- No cross-org leaks
```

**Success criteria:** <100ms queries, zero leaks  
**Timeline:** Day 1

---

## WEEK 3-4 TIMELINE

### Week 3: Build Prototype
**Mon-Tue:** Spikes (PDF, rules, RLS)  
**Wed-Thu:** v0 UI generation (flows)  
**Fri:** Deploy staging, internal testing

### Week 4: Validate + Iterate
**Mon:** Share with 3 agencies, collect feedback  
**Tue-Wed:** AI-driven iterations  
**Thu:** Final demo-ready state  
**Fri:** Go/no-go decision for Sprint 1

---

## SUCCESS CRITERIA

### Technical Feasibility
- [x] PDF generation works (<10 sec)
- [x] Rules engine handles 20 scenarios
- [x] RLS enforces permissions correctly

### Product Validation (Pilot Agencies)
- [x] 3/3 agencies complete onboarding flow
- [x] Average rating ≥7/10 ("would use this")
- [x] <3 critical UX issues identified

### Go/No-Go Decision
**GO if:** All technical + 2/3 product criteria met  
**ITERATE if:** Technical pass but UX issues  
**PIVOT if:** Technical blockers or <5/10 rating

---

## TOOLS & COST

**Development:**
- v0.dev: $20/month
- Claude Pro: $20/month
- Supabase: Free tier
- Vercel: Free tier

**Total: $40 for 2 weeks**

---

## OUTPUTS

1. **Working prototype:** staging.sanzu.ai
2. **PDF samples:** 3 professional templates
3. **Validation report:** Agency feedback + decision
4. **Technical confidence:** Spikes documented

**Next phase:** Sprint 1 (Week 5) if GO

